using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HackerNewsAPI.Application.Queries;
using HackerNewsAPI.Domain.ValueObjects;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.Entities;

namespace HackerNewsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class StoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IStoryService _storyService;
    private readonly ILogger<StoriesController> _logger;

    public StoriesController(IMediator mediator, IStoryService storyService, ILogger<StoriesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _storyService = storyService ?? throw new ArgumentNullException(nameof(storyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieve a specific story by its ID
    /// </summary>
    /// <param name="id">Story identifier</param>
    /// <param name="cancellationToken">Request cancellation token</param>
    /// <returns>Story details or 404 if not found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Story), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Story>> GetStoryById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing request for story {StoryId}", id);
            var story = await _storyService.GetStoryByIdAsync(id, cancellationToken);
            
            if (story == null)
            {
                _logger.LogWarning("Story {StoryId} not found", id);
                return NotFound($"Story with ID {id} not found");
            }
            
            return Ok(story);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for story {StoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Get the top stories from Hacker News
    /// </summary>
    /// <param name="count">Number of stories to return (max 100)</param>
    /// <param name="cancellationToken">Request cancellation token</param>
    /// <returns>Stories sorted by score (highest first)</returns>
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
            var stories = await _storyService.GetBestStoriesAsync(count, cancellationToken);
            return Ok(stories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request for best stories");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
        }
    }
}
