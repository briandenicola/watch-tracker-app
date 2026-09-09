using Microsoft.AspNetCore.Http;
using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Services;

public interface IExternalDataImportService
{
    Task<ExternalImportPreviewDto> PreviewAsync(
        int userId,
        IFormFile file,
        CancellationToken ct = default);

    Task<ExternalImportResultDto> ImportAsync(
        int userId,
        IFormFile file,
        IReadOnlySet<int> selectedRows,
        CancellationToken ct = default);
}
