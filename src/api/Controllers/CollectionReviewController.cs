using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WatchTracker.Api.DTOs;
using WatchTracker.Api.Services;

namespace WatchTracker.Api.Controllers;

[ApiController]
[Route("api/collection/review")]
[Authorize]
public class CollectionReviewController(
    ICollectionReviewService review,
    ICollectionReviewCandidateService candidates,
    ILogger<CollectionReviewController> logger) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Whether a review can be generated, and the stored one when there is one.
    /// A user who has never run one still needs the configured flag, so this
    /// answers with state rather than nothing.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CollectionReviewStateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionReviewStateDto>> GetLatest(CancellationToken ct) =>
        Ok(await review.GetStateAsync(UserId, ct));

    [HttpPost]
    [EnableRateLimiting("collection-review")]
    [ProducesResponseType(typeof(CollectionReviewStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionReviewStateDto>> Generate(CancellationToken ct)
    {
        try
        {
            return Ok(await review.GenerateAsync(UserId, ct));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("A collection review request was rejected.");
            logger.LogDebug(
                ex,
                "Collection review for user {UserId} was rejected: {Reason}",
                UserId,
                ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Buyable watches filling the gaps the stored review identified.</summary>
    [HttpPost("candidates")]
    [EnableRateLimiting("collection-review")]
    [ProducesResponseType(typeof(CollectionReviewCandidatesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollectionReviewCandidatesDto>> GenerateCandidates(
        GenerateCandidatesDto dto,
        CancellationToken ct)
    {
        try
        {
            return Ok(await candidates.GenerateAsync(UserId, dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            // Same reason as the review above: a rejected candidate search was
            // invisible in the log, which is where this gets diagnosed.
            logger.LogWarning("A candidate search request was rejected.");
            logger.LogDebug(
                ex,
                "Candidate search for user {UserId} was rejected: {Reason}",
                UserId,
                ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("candidates/wishlist")]
    [ProducesResponseType(typeof(AdvisorWishlistActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AdvisorWishlistActionResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdvisorWishlistActionResultDto>> AddCandidateToWishlist(
        CandidateWishlistActionDto dto,
        CancellationToken ct)
    {
        var result = await candidates.AddToWishlistAsync(UserId, dto, ct);
        if (result is null) return NotFound();
        return result.Added
            ? Created($"/api/watches/{result.WatchId}", result)
            : Ok(result);
    }
}
