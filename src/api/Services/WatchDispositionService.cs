using Microsoft.EntityFrameworkCore;
using WatchTracker.Api.Data;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.Services;

public class WatchDispositionService(AppDbContext context) : IWatchDispositionService
{
    public Task<WatchDto?> RetireAsync(int id, int userId, CancellationToken ct = default) =>
        SetDispositionAsync(id, userId, new UpdateWatchDispositionDto
        {
            Type = DispositionType.Retired,
            DispositionDate = DateTime.UtcNow,
        }, ct);

    public Task<WatchDto?> UnretireAsync(int id, int userId, CancellationToken ct = default) =>
        ClearDispositionAsync(id, userId, ct);

    public async Task<WatchDto?> SetDispositionAsync(
        int id,
        int userId,
        UpdateWatchDispositionDto dto,
        CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .Include(w => w.Disposition)
                .ThenInclude(d => d!.ReceivedWatch)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);
        if (watch is null) return null;
        if (watch.IsWishList)
            throw new InvalidOperationException("A wish list watch cannot have a disposition.");

        Watch? receivedWatch = null;
        if (dto.Type == DispositionType.Traded && dto.ReceivedWatchId is int receivedWatchId)
        {
            if (receivedWatchId == id)
                throw new InvalidOperationException("A watch cannot be traded for itself.");

            receivedWatch = await context.Watches
                .FirstOrDefaultAsync(w => w.Id == receivedWatchId && w.UserId == userId && !w.IsWishList, ct)
                ?? throw new InvalidOperationException("The selected received watch was not found.");
        }

        var disposition = watch.Disposition ?? new WatchDisposition { WatchId = watch.Id };
        disposition.Type = dto.Type;
        disposition.DispositionDate = dto.DispositionDate;
        disposition.Notes = NullIfWhiteSpace(dto.Notes);
        disposition.SoldTo = dto.Type == DispositionType.Sold ? NullIfWhiteSpace(dto.SoldTo) : null;
        disposition.SalePrice = dto.Type == DispositionType.Sold ? dto.SalePrice : null;
        disposition.ReceivedWatchId = dto.Type == DispositionType.Traded ? dto.ReceivedWatchId : null;
        disposition.ReceivedWatch = receivedWatch;
        disposition.TradeDetails = dto.Type == DispositionType.Traded
            ? NullIfWhiteSpace(dto.TradeDetails) ?? (receivedWatch is null ? null : $"{receivedWatch.Brand} {receivedWatch.Model}")
            : null;
        disposition.OtherLabel = dto.Type == DispositionType.Other ? NullIfWhiteSpace(dto.OtherLabel) : null;
        disposition.ReturnReason = dto.Type == DispositionType.Returned ? NullIfWhiteSpace(dto.ReturnReason) : null;
        disposition.ReturnedTo = dto.Type == DispositionType.Returned ? NullIfWhiteSpace(dto.ReturnedTo) : null;
        disposition.RefundAmount = dto.Type == DispositionType.Returned ? dto.RefundAmount : null;

        if (watch.Disposition is null)
        {
            context.WatchDispositions.Add(disposition);
            watch.Disposition = disposition;
        }

        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return WatchDtoMapper.Map(watch);
    }

    public async Task<WatchDto?> ClearDispositionAsync(int id, int userId, CancellationToken ct = default)
    {
        var watch = await context.Watches
            .Include(w => w.Images)
            .Include(w => w.Disposition)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);
        if (watch is null) return null;

        if (watch.Disposition is not null)
        {
            context.WatchDispositions.Remove(watch.Disposition);
            watch.Disposition = null;
        }

        watch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return WatchDtoMapper.Map(watch);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
