namespace WatchTracker.Api.DTOs;

public class ImportResultDto
{
    public int WatchesImported { get; set; }
    public int ImagesImported { get; set; }
    public int WearLogsImported { get; set; }
}

public sealed class DataImportOutcome
{
    public ImportResultDto? Result { get; private init; }
    public string? Error { get; private init; }

    public static DataImportOutcome Success(ImportResultDto result) => new() { Result = result };
    public static DataImportOutcome Failure(string error) => new() { Error = error };
}
