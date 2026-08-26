using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IWishlistExtractionService
{
    Task<WishlistExtractionResultDto> ExtractAsync(string url, CancellationToken ct = default);
}
