using Microsoft.AspNetCore.Http;

namespace WatchTracker.Api.DTOs;

public class ExternalImportPreviewDto
{
    public required string Source { get; set; }
    public int CollectionCount { get; set; }
    public int WishlistCount { get; set; }
    public int DisposedCount { get; set; }
    public int DuplicateCount { get; set; }
    public List<ExternalImportRowDto> Rows { get; set; } = [];
}

public class ExternalImportRowDto
{
    public int RowNumber { get; set; }
    public required string Brand { get; set; }
    public required string Model { get; set; }
    public required string Destination { get; set; }
    public bool CanImport { get; set; }
    public bool IsDuplicate { get; set; }
    public int? DuplicateWatchId { get; set; }
    public string? DuplicateReason { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public class ExternalImportRequestDto
{
    public required IFormFile File { get; set; }
    public string SelectedRows { get; set; } = "";
}

public class ExternalImportResultDto
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int DuplicatesImported { get; set; }
}
