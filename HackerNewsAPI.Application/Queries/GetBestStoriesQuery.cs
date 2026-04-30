using MediatR;
using HackerNewsAPI.Domain.ValueObjects;

namespace HackerNewsAPI.Application.Queries;

public record GetBestStoriesQuery(int Count) : IRequest<IEnumerable<StoryDto>>;
