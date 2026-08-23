using System.ComponentModel.DataAnnotations;

namespace WatchTracker.Api.DTOs;

/// <summary>A value the analysis proposes for one empty field. Nothing is written until the owner says so.</summary>
public class WatchFieldSuggestionDto
{
    public required string Field { get; set; }
    public required string Label { get; set; }

    /// <summary>"text", "number" or "integer".</summary>
    public required string Kind { get; set; }

    public required string Value { get; set; }

    /// <summary>"high", "medium" or "low", as claimed by the model.</summary>
    public required string Confidence { get; set; }

    /// <summary>A short note on what in the photo led to the value.</summary>
    public string? Reason { get; set; }
}

public class WatchAnalysisResultDto
{
    /// <summary>The short description of the watch, saved to the watch as before.</summary>
    public required string Summary { get; set; }

    public List<WatchFieldSuggestionDto> Suggestions { get; set; } = [];
}

public class ApplyAnalysisSuggestionsDto
{
    /// <summary>Field name to approved value, as edited by the owner.</summary>
    [Required]
    public Dictionary<string, string> Values { get; set; } = [];
}

public class ApplyAnalysisResultDto
{
    public List<string> Applied { get; set; } = [];

    /// <summary>Anything the server would not write, and why — an unknown field, or a value it could not parse.</summary>
    public List<string> Rejected { get; set; } = [];

    public required WatchDto Watch { get; set; }
}
