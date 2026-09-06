using System.Diagnostics;
using System.Net;

namespace WatchTracker.Api.Services;

/// <summary>
/// One place for the mechanics every Ollama-backed feature repeats: send the
/// request, keep an unreachable provider from surfacing as an unexplained 500,
/// and leave behind the lines a failure is diagnosed from. Each caller keeps its
/// own parsing and its own wording for a rejected request — only the transport
/// and its logging live here.
///
/// Levels follow the rule the rest of the app uses: Information and Warning carry
/// status, timing and sizes; Debug — an operator asking to see everything — adds
/// the provider URL, the prompt and the provider's own response body.
/// </summary>
public static class OllamaChat
{
    public static async Task<OllamaChatResult> SendAsync(
        HttpClient httpClient,
        HttpRequestMessage request,
        ILogger logger,
        string feature,
        string ollamaUrl,
        string? prompt,
        CancellationToken ct)
    {
        var timer = Stopwatch.StartNew();
        if (prompt is not null && logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug(
                "Calling Ollama at {OllamaUrl} for the {Feature} with a {PromptLength}-character prompt: {Prompt}",
                ollamaUrl,
                feature,
                prompt.Length,
                LogText.Bounded(prompt));

        HttpResponseMessage response;
        string body;
        try
        {
            response = await httpClient.SendAsync(request, ct);
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // A timeout or a caller hanging up belongs to the caller, which knows
            // which of the two it is and what to say about it.
            throw;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(
                "The {Feature} could not reach the configured model provider after {DurationMs} ms ({ErrorType}).",
                feature,
                timer.ElapsedMilliseconds,
                ex.GetType().Name);
            logger.LogDebug(ex, "The {Feature} request to {OllamaUrl} failed.", feature, ollamaUrl);
            throw new InvalidOperationException(
                $"The {feature} could not reach Ollama. Check the Ollama URL under Admin -> Settings.");
        }

        var status = response.StatusCode;
        var success = response.IsSuccessStatusCode;
        response.Dispose();

        if (!success)
        {
            logger.LogWarning(
                "Ollama returned HTTP {StatusCode} for the {Feature} after {DurationMs} ms "
                + "({ResponseLength} characters).",
                (int)status,
                feature,
                timer.ElapsedMilliseconds,
                body.Length);
            logger.LogDebug(
                "Ollama at {OllamaUrl} rejected the {Feature}: {ResponseBody}",
                ollamaUrl,
                feature,
                LogText.Bounded(body));
            return new OllamaChatResult(status, false, body);
        }

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug(
                "Ollama at {OllamaUrl} answered the {Feature} in {DurationMs} ms with {ResponseLength} "
                + "characters: {ModelReply}",
                ollamaUrl,
                feature,
                timer.ElapsedMilliseconds,
                body.Length,
                LogText.Bounded(body));

        return new OllamaChatResult(status, true, body);
    }
}

public readonly record struct OllamaChatResult(HttpStatusCode StatusCode, bool IsSuccess, string Body);
