using System.ComponentModel.DataAnnotations;
using WatchTracker.Api.DTOs;

namespace WatchTracker.Api.Tests;

public class WatchFieldValidationTests
{
    [Fact]
    public void Create_and_update_share_watch_field_validation()
    {
        AssertInvalidCaseSize(new CreateWatchDto
        {
            Brand = "Seiko",
            Model = "SPB143",
            CaseSizeMm = 0,
        });
        AssertInvalidCaseSize(new UpdateWatchDto
        {
            Brand = "Seiko",
            Model = "SPB143",
            CaseSizeMm = 0,
        });
    }

    private static void AssertInvalidCaseSize(object dto)
    {
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(WatchFieldsDto.CaseSizeMm)));
    }
}
