using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HackerNewsAPI.Application.Queries;
using HackerNewsAPI.Domain.ValueObjects;

namespace HackerNewsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class StoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StoriesController> _logger;

    public StoriesController(IMediator mediator, ILogger<StoriesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the best n stories from Hacker News
    /// </summary>
    /// <param name="count">Number of stories to retrieve (default: 10, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of stories ordered by score in descending order</returns>
    [HttpGet("best")]
    [ProducesResponseType(typeof(IEnumerable<StoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<StoryDto>>> GetBestStories([FromQuery] int count = 10, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            _logger.LogWarning("Invalid count parameter: {Count}", count);
            return BadRequest("Count must be a positive number");
        }

        if (count > 100)
        {
            _logger.LogWarning("Count parameter too high: {Count}", count);
            return BadRequest("Count cannot exceed 100");
        }

        try
        {
            _logger.LogInformation("Processing request for {Count} best stories", count);
            var query = new GetBestStoriesQuery(count);
            var stories = await _mediator.Send(query, cancellationToken);
            return Ok(stories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for best stories");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
        }
    }
}
