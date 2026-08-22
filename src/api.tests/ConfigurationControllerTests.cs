using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.Controllers;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Tests;

public class ConfigurationControllerTests
{
    [Fact]
    public async Task Get_returns_effective_application_timezone()
    {
        var controller = new ConfigurationController(
            new StubAppSettingsService("America/Chicago"));

        var result = await controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var configuration = Assert.IsType<ApplicationConfigurationDto>(ok.Value);
        Assert.Equal("America/Chicago", configuration.TimeZone);
    }

    [Theory]
    [InlineData("America/Chicago", true)]
    [InlineData("UTC", true)]
    [InlineData("", false)]
    [InlineData("Not/A_Timezone", false)]
    [InlineData("Eastern Standard Time", false)]
    public void Timezone_validation_rejects_unknown_ids(string value, bool expected)
    {
        Assert.Equal(expected, AppSettingsService.IsValidTimeZoneId(value));
    }

    private sealed class StubAppSettingsService(string timeZone) : IAppSettingsService
    {
        public Task<string> GetAsync(string key, string defaultValue = "") =>
            Task.FromResult(timeZone);

        public Task<int> GetIntAsync(string key, int defaultValue) =>
            throw new NotSupportedException();

        public Task SetAsync(string key, string value) =>
            throw new NotSupportedException();

        public Task<Dictionary<string, string>> GetAllAsync() =>
            throw new NotSupportedException();
    }
}
