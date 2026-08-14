namespace WatchTracker.Api.Models;

public class WatchDisposition
{
    public int WatchId { get; set; }
    public DispositionType Type { get; set; }
    public DateTime DispositionDate { get; set; }
    public string? Notes { get; set; }
    public string? SoldTo { get; set; }
    public decimal? SalePrice { get; set; }
    public int? ReceivedWatchId { get; set; }
    public string? TradeDetails { get; set; }
    public string? OtherLabel { get; set; }
    public string? ReturnReason { get; set; }
    public string? ReturnedTo { get; set; }
    public decimal? RefundAmount { get; set; }

    public Watch Watch { get; set; } = null!;
    public Watch? ReceivedWatch { get; set; }
}
