using MediatR;
using Microsoft.Extensions.Logging;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Application.Queries;
using HackerNewsAPI.Domain.ValueObjects;

namespace HackerNewsAPI.Application.Handlers;

public class GetBestStoriesQueryHandler : IRequestHandler<GetBestStoriesQuery, IEnumerable<StoryDto>>
{
    private readonly IStoryService _storyService;
    private readonly ILogger<GetBestStoriesQueryHandler> _logger;

    public GetBestStoriesQueryHandler(IStoryService storyService, ILogger<GetBestStoriesQueryHandler> logger)
    {
        _storyService = storyService ?? throw new ArgumentNullException(nameof(storyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<StoryDto>> Handle(GetBestStoriesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetBestStoriesQuery for {Count} stories", request.Count);

        try
        {
            return await _storyService.GetBestStoriesAsync(request.Count, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GetBestStoriesQuery");
            throw;
        }
    }
}
