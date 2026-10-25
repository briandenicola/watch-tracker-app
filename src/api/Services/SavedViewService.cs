using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class SavedViewService(AppDbContext context) : ISavedViewService
{
    public const int PinLimit = 5;

    private static readonly HashSet<string> CollectionSorts = ["dateAdded", "brand", "lastWorn", "timesWorn"];
    private static readonly HashSet<string> WishlistSorts = [.. CollectionSorts, "priority"];

    public async Task<List<SavedViewDto>> GetAllAsync(int userId, CancellationToken ct = default)
    {
        var views = await context.SavedViews
            .Include(v => v.Labels)
            .Where(v => v.UserId == userId)
            .ToListAsync(ct);

        // Pinned views first in the order they were pinned, then the rest by name.
        return views
            .OrderBy(v => v.PinnedAt is null)
            .ThenBy(v => v.PinnedAt)
            .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .Select(Map)
            .ToList();
    }

    public async Task<SavedViewDto> CreateAsync(int userId, SaveSavedViewDto dto, CancellationToken ct = default)
    {
        var view = new SavedView { UserId = userId, Name = dto.Name.Trim() };
        await ApplyAsync(view, userId, dto, ct);
        if (dto.Pinned) await PinAsync(view, userId, ct);

        context.SavedViews.Add(view);
        await context.SaveChangesAsync(ct);
        return Map(view);
    }

    public async Task<SavedViewDto?> UpdateAsync(int id, int userId, SaveSavedViewDto dto, CancellationToken ct = default)
    {
        var view = await FindAsync(id, userId, ct);
        if (view is null) return null;

        await ApplyAsync(view, userId, dto, ct);
        if (dto.Pinned && view.PinnedAt is null) await PinAsync(view, userId, ct);
        if (!dto.Pinned) view.PinnedAt = null;
        view.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return Map(view);
    }

    public async Task<SavedViewDto?> SetPinnedAsync(int id, int userId, bool pinned, CancellationToken ct = default)
    {
        var view = await FindAsync(id, userId, ct);
        if (view is null) return null;

        if (pinned && view.PinnedAt is null) await PinAsync(view, userId, ct);
        if (!pinned) view.PinnedAt = null;
        await context.SaveChangesAsync(ct);
        return Map(view);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
    {
        var view = await context.SavedViews.FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId, ct);
        if (view is null) return false;

        context.SavedViews.Remove(view);
        await context.SaveChangesAsync(ct);
        return true;
    }

    private Task<SavedView?> FindAsync(int id, int userId, CancellationToken ct) =>
        context.SavedViews
            .Include(v => v.Labels)
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId, ct);

    private async Task ApplyAsync(SavedView view, int userId, SaveSavedViewDto dto, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        if (name.Length == 0)
            throw new ArgumentException("View name is required.");

        var sorts = dto.Scope == SavedViewScope.Wishlist ? WishlistSorts : CollectionSorts;
        if (dto.SortBy is not null && !sorts.Contains(dto.SortBy))
            throw new ArgumentException($"\"{dto.SortBy}\" is not a sort option for this list.");

        var lower = name.ToLower();
        var taken = await context.SavedViews.AnyAsync(
            v => v.UserId == userId && v.Id != view.Id && v.Name.ToLower() == lower, ct);
        if (taken)
            throw new InvalidOperationException($"You already have a view called \"{name}\".");

        var labels = await LabelService.LoadOwnedLabelsAsync(context, userId, dto.LabelIds, ct);
        view.Name = name;
        view.Scope = dto.Scope;
        view.LabelMatch = dto.LabelMatch;
        view.Brand = string.IsNullOrWhiteSpace(dto.Brand) ? null : dto.Brand.Trim();
        view.MovementType = dto.MovementType;
        view.SortBy = dto.SortBy;
        view.Labels.Clear();
        foreach (var label in labels) view.Labels.Add(label);
    }

    private async Task PinAsync(SavedView view, int userId, CancellationToken ct)
    {
        var pinned = await context.SavedViews.CountAsync(
            v => v.UserId == userId && v.Id != view.Id && v.PinnedAt != null, ct);
        if (pinned >= PinLimit)
            throw new InvalidOperationException($"You can pin up to {PinLimit} views. Unpin one first.");
        view.PinnedAt = DateTime.UtcNow;
    }

    private static SavedViewDto Map(SavedView view) => new()
    {
        Id = view.Id,
        Name = view.Name,
        Scope = view.Scope,
        LabelIds = view.Labels.Select(l => l.Id).Order().ToList(),
        LabelMatch = view.LabelMatch,
        Brand = view.Brand,
        MovementType = view.MovementType,
        SortBy = view.SortBy,
        Pinned = view.PinnedAt is not null,
        PinnedAt = view.PinnedAt,
    };
}
