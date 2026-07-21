using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class ResaleValueEntryDto
{
    public int Id { get; set; }
    public int WatchId { get; set; }
    public decimal Value { get; set; }
    public ResaleValueSource Source { get; set; }
    public string? Reasoning { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class CreateResaleValueEntryDto
{
    [Range(0, 10_000_000)]
    public decimal Value { get; set; }

    public DateTime? RecordedAt { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class ResaleRefreshSummaryDto
{
    public int Due { get; set; }
    public int Refreshed { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
}
