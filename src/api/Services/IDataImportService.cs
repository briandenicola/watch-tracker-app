using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IDataImportService
{
    Task<DataImportOutcome> ImportAsync(int userId, IFormFile file, CancellationToken ct = default);
}
