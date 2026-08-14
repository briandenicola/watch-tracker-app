using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.Models;

namespace WatchTracker.Api.DTOs;

public class UpdateWatchDispositionDto : IValidatableObject
{
    [EnumDataType(typeof(DispositionType))]
    public DispositionType Type { get; set; }
    public DateTime DispositionDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    [StringLength(200)]
    public string? SoldTo { get; set; }

    [Range(0, 10_000_000)]
    public decimal? SalePrice { get; set; }

    public int? ReceivedWatchId { get; set; }

    [StringLength(2000)]
    public string? TradeDetails { get; set; }

    [StringLength(100)]
    public string? OtherLabel { get; set; }

    [StringLength(2000)]
    public string? ReturnReason { get; set; }

    [StringLength(200)]
    public string? ReturnedTo { get; set; }

    [Range(0, 10_000_000)]
    public decimal? RefundAmount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(Type))
            yield return new ValidationResult("Disposition type is invalid.", [nameof(Type)]);

        if (DispositionDate == default)
            yield return new ValidationResult("Disposition date is required.", [nameof(DispositionDate)]);

        if (Type == DispositionType.Sold)
        {
            if (string.IsNullOrWhiteSpace(SoldTo))
                yield return new ValidationResult("Buyer is required for a sold watch.", [nameof(SoldTo)]);
            if (SalePrice is null)
                yield return new ValidationResult("Sale price is required for a sold watch.", [nameof(SalePrice)]);
        }

        if (Type == DispositionType.Traded
            && ReceivedWatchId is null
            && string.IsNullOrWhiteSpace(TradeDetails))
        {
            yield return new ValidationResult(
                "Select a received watch or describe what was received.",
                [nameof(ReceivedWatchId), nameof(TradeDetails)]);
        }

        if (Type == DispositionType.Returned && string.IsNullOrWhiteSpace(ReturnReason))
            yield return new ValidationResult("Return reason is required.", [nameof(ReturnReason)]);

        if (Type == DispositionType.Other && string.IsNullOrWhiteSpace(OtherLabel))
            yield return new ValidationResult("A disposition label is required.", [nameof(OtherLabel)]);
    }
}

public class WatchDispositionDto
{
    public DispositionType Type { get; set; }
    public DateTime DispositionDate { get; set; }
    public string? Notes { get; set; }
    public string? SoldTo { get; set; }
    public decimal? SalePrice { get; set; }
    public int? ReceivedWatchId { get; set; }
    public string? ReceivedWatchName { get; set; }
    public string? TradeDetails { get; set; }
    public string? OtherLabel { get; set; }
    public string? ReturnReason { get; set; }
    public string? ReturnedTo { get; set; }
    public decimal? RefundAmount { get; set; }
}
