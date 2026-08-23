using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchAnalysisService(
    AppDbContext context,
    IAppSettingsService appSettings,
    IWatchService watchService,
    HttpClient httpClient,
    IWebHostEnvironment env,
    ILogger<WatchAnalysisService> logger) : IWatchAnalysisService
{
    private const string AnalysisNotesSeparator = "\n\n---\n\n## AI Analysis\n\n";

    public async Task<WatchAnalysisResultDto> AnalyzeAsync(int watchId, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct)
            ?? throw new InvalidOperationException("Watch not found.");

        var image = watch.Images.OrderBy(i => i.SortOrder).FirstOrDefault()
            ?? throw new InvalidOperationException("No images to analyze.");

        var filePath = Path.Combine(env.ContentRootPath, "uploads", image.FileName);
        if (!File.Exists(filePath))
            throw new InvalidOperationException("Image file not found.");

        var imageBytes = await File.ReadAllBytesAsync(filePath, ct);
        var base64 = Convert.ToBase64String(imageBytes);

        var missing = SuggestibleWatchFields.MissingOn(watch);
        var prompt = await BuildPromptAsync(missing);

        var content = await AnalyzeWithOllamaAsync(base64, prompt, ct);
        var result = ParseAnalysis(content, missing);

        watch.AiAnalysis = result.Summary;
        watch.Notes = MergeAnalysisIntoNotes(watch.Notes, result.Summary);
        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return result;
    }

    public async Task<ApplyAnalysisResultDto?> ApplySuggestionsAsync(
        int watchId, int userId, ApplyAnalysisSuggestionsDto dto, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);
        if (watch is null) return null;

        var applied = new List<string>();
        var rejected = new List<string>();

        foreach (var (name, value) in dto.Values)
        {
            var field = SuggestibleWatchFields.Find(name);
            if (field is null)
            {
                // Only the allow-list is writable this way, whatever the client sends.
                rejected.Add($"{name} is not a field the analysis can fill in");
                continue;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                rejected.Add($"{field.Label} was empty");
                continue;
            }

            var problem = field.Apply(watch, value);
            if (problem is null) applied.Add(field.Label);
            else rejected.Add($"{field.Label} {problem}");
        }

        if (applied.Count > 0)
        {
            watch.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            logger.LogInformation(
                "Watch {WatchId} updated from AI suggestions by user {UserId}: {Fields}",
                watchId, userId, string.Join(", ", applied));
        }

        var updated = await watchService.GetByIdAsync(watchId, userId, ct);
        if (updated is null) return null;

        return new ApplyAnalysisResultDto
        {
            Applied = applied,
            Rejected = rejected,
            Watch = updated
        };
    }

    // --- Prompt ------------------------------------------------------------

    private async Task<string> BuildPromptAsync(IReadOnlyList<SuggestibleWatchField> missing)
    {
        var persona = await appSettings.GetAsync(
            AppSettingsService.Keys.AiAnalysisPrompt,
            "You are a watch expert. Describe the watch in this photo.");

        var prompt = new StringBuilder();
        prompt.AppendLine(persona.Trim());
        prompt.AppendLine();
        prompt.AppendLine(Contract);
        prompt.AppendLine();

        if (missing.Count == 0)
        {
            prompt.AppendLine(
                "This record already has every field you are allowed to fill in, so return an empty \"suggestions\" array.");
        }
        else
        {
            prompt.AppendLine("Fields this record is missing — the only ones you may suggest, by these exact names:");
            foreach (var field in missing)
                prompt.AppendLine($"- {field.Name}: {field.Hint}");
        }

        return prompt.ToString();
    }

    private const string Contract = """
        Answer with a single JSON object and nothing else, in exactly this shape:
        {"summary": "<your description>", "suggestions": [{"field": "<exact field name>", "value": "<the value>", "confidence": "high|medium|low", "reason": "<what in the photo tells you>"}]}

        - "summary": describe the watch as you see it — brand and model if the dial is legible, case, dial, hands, bezel and strap, plus anything notable. Under 70 words, plain prose, no headings or bullet points. This is the whole description, so make it count.
        - "suggestions": propose a value only for the fields listed below, using the exact field name given. Leave a field out rather than guessing — a blank record is better than a wrong one, and the owner has to approve every value anyway.
        - "reason": a few words, not a sentence.
        - "confidence": "high" only when the photo or the printing on the dial settles it; "low" when you are inferring from the brand or the style.
        """;

    // --- Reading the model's answer ----------------------------------------

    private static WatchAnalysisResultDto ParseAnalysis(
        string content, IReadOnlyList<SuggestibleWatchField> missing)
    {
        var json = OllamaJson.ExtractObject(content);
        if (json is null) return new WatchAnalysisResultDto { Summary = content.Trim() };

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new WatchAnalysisResultDto { Summary = content.Trim() };

            var summary = root.TryGetProperty("summary", out var summaryElement)
                && summaryElement.ValueKind == JsonValueKind.String
                    ? summaryElement.GetString()?.Trim()
                    : null;

            var result = new WatchAnalysisResultDto
            {
                Summary = string.IsNullOrWhiteSpace(summary) ? content.Trim() : summary
            };

            if (!root.TryGetProperty("suggestions", out var suggestions)
                || suggestions.ValueKind != JsonValueKind.Array)
                return result;

            // A scratch watch lets a value be tried out — parsed and range
            // checked by the field's own rule — without touching the real one.
            var scratch = new Watch { Brand = "", Model = "" };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in suggestions.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                var name = ReadString(item, "field");
                var value = ReadString(item, "value");
                if (name is null || value is null || !seen.Add(name)) continue;

                // Only fields that were actually missing: the model does not get
                // to talk the owner into overwriting something already recorded.
                var field = missing.FirstOrDefault(f =>
                    string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
                if (field is null) continue;

                if (field.Apply(scratch, value) is not null) continue;

                result.Suggestions.Add(new WatchFieldSuggestionDto
                {
                    Field = field.Name,
                    Label = field.Label,
                    Kind = field.Kind,
                    Value = value.Trim(),
                    Confidence = NormalizeConfidence(ReadString(item, "confidence")),
                    Reason = ReadString(item, "reason")
                });
            }

            return result;
        }
        catch (JsonException)
        {
            return new WatchAnalysisResultDto { Summary = content.Trim() };
        }
    }

    private static string NormalizeConfidence(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "high" => "high",
        "low" => "low",
        _ => "medium"
    };

    private static string? ReadString(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var element)) return null;

        var text = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            // Models answer a number field with a bare number often enough to allow it.
            JsonValueKind.Number => element.ToString(),
            _ => null
        };

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    // --- Ollama ------------------------------------------------------------

    private async Task<string> AnalyzeWithOllamaAsync(string base64, string prompt, CancellationToken ct)
    {
        var ollamaUrl = await appSettings.GetAsync(AppSettingsService.Keys.OllamaUrl, "http://localhost:11434");
        if (string.IsNullOrWhiteSpace(ollamaUrl))
            throw new InvalidOperationException("Ollama URL is not configured.");

        var model = await appSettings.GetAsync(AppSettingsService.Keys.OllamaModel);
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Ollama model is not configured.");

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt,
                    images = new[] { base64 }
                }
            },
            format = "json",
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{ollamaUrl.TrimEnd('/')}/api/chat")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama API error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement.GetProperty("message");
        var analysis = message.GetProperty("content").GetString()
            ?? throw new InvalidOperationException("No content in Ollama response.");

        return analysis;
    }

    private static string MergeAnalysisIntoNotes(string? notes, string analysis)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return analysis;

        var existingNotes = notes.TrimEnd();
        var existingAnalysisStart = existingNotes.IndexOf(AnalysisNotesSeparator, StringComparison.Ordinal);
        if (existingAnalysisStart >= 0)
            existingNotes = existingNotes[..existingAnalysisStart].TrimEnd();

        if (string.Equals(existingNotes.Trim(), analysis.Trim(), StringComparison.Ordinal))
            return analysis;

        if (string.IsNullOrWhiteSpace(existingNotes))
            return analysis;

        return $"{existingNotes}{AnalysisNotesSeparator}{analysis}";
    }

    public async Task<List<string>> GetOllamaModelsAsync(string url, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{url.TrimEnd('/')}/api/tags");
        var response = await httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to connect to Ollama at {url}");

        using var doc = JsonDocument.Parse(responseBody);
        var models = doc.RootElement.GetProperty("models");
        return models.EnumerateArray()
            .Select(m => m.GetProperty("name").GetString()!)
            .OrderBy(n => n)
            .ToList();
    }
}
