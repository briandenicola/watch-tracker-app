using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/configuration")]
[Authorize]
public class ConfigurationController(IAppSettingsService appSettings) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApplicationConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApplicationConfigurationDto>> Get()
    {
        var timeZone = await appSettings.GetAsync(AppSettingsService.Keys.ApplicationTimeZone);
        return Ok(new ApplicationConfigurationDto { TimeZone = timeZone });
    }
}
