using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class LabelService(AppDbContext context) : ILabelService
{
    public const string DefaultColor = "#6b7280";

    public async Task<List<LabelDto>> GetAllAsync(int userId, CancellationToken ct = default) =>
        await context.Labels
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelDto { Id = l.Id, Name = l.Name, Color = l.Color, WatchCount = l.Watches.Count })
            .ToListAsync(ct);

    public async Task<LabelDto> CreateAsync(int userId, SaveLabelDto dto, CancellationToken ct = default)
    {
        var name = dto.Name.Trim();
        await EnsureNameIsFreeAsync(userId, name, null, ct);

        var label = new Label { UserId = userId, Name = name, Color = dto.Color ?? DefaultColor };
        context.Labels.Add(label);
        await context.SaveChangesAsync(ct);
        return new LabelDto { Id = label.Id, Name = label.Name, Color = label.Color };
    }

    public async Task<LabelDto?> UpdateAsync(int id, int userId, SaveLabelDto dto, CancellationToken ct = default)
    {
        var label = await context.Labels.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);
        if (label is null) return null;

        var name = dto.Name.Trim();
        await EnsureNameIsFreeAsync(userId, name, id, ct);
        label.Name = name;
        if (dto.Color is not null) label.Color = dto.Color;
        await context.SaveChangesAsync(ct);

        var count = await context.Watches.CountAsync(w => w.Labels.Any(l => l.Id == id), ct);
        return new LabelDto { Id = label.Id, Name = label.Name, Color = label.Color, WatchCount = count };
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var label = await context.Labels.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId, ct);
        if (label is null) return false;

        // Watch and saved view links are removed by the join tables' cascade.
        context.Labels.Remove(label);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<int>?> SetWatchLabelsAsync(
        int watchId, int userId, IReadOnlyCollection<int> labelIds, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Labels)
            .FirstOrDefaultAsync(w => w.Id == watchId && w.UserId == userId, ct);
        if (watch is null) return null;

        var labels = await LoadOwnedLabelsAsync(context, userId, labelIds, ct);
        watch.Labels.Clear();
        foreach (var label in labels) watch.Labels.Add(label);
        await context.SaveChangesAsync(ct);
        return labels.Select(l => l.Id).Order().ToList();
    }

    /// <summary>Loads the user's labels with these ids, rejecting any id that is not theirs.</summary>
    internal static async Task<List<Label>> LoadOwnedLabelsAsync(
        AppDbContext context, int userId, IReadOnlyCollection<int> labelIds, CancellationToken ct)
    {
        var ids = labelIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var labels = await context.Labels
            .Where(l => l.UserId == userId && ids.Contains(l.Id))
            .ToListAsync(ct);
        if (labels.Count != ids.Count)
            throw new ArgumentException("One or more labels do not exist.");
        return labels;
    }

    private async Task EnsureNameIsFreeAsync(int userId, string name, int? exceptId, CancellationToken ct)
    {
        if (name.Length == 0)
            throw new ArgumentException("Label name is required.");

        var lower = name.ToLower();
        var taken = await context.Labels.AnyAsync(
            l => l.UserId == userId && l.Id != exceptId && l.Name.ToLower() == lower, ct);
        if (taken)
            throw new InvalidOperationException($"You already have a label called \"{name}\".");
    }
}
