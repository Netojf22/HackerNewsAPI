# Hacker News API

Hey there! This is a clean, well-structured ASP.NET Core API that fetches the top stories from Hacker News. I built this using Domain-Driven Design principles and Test-Driven Development to show how a real-world API should be structured.

## What's Inside?

- **Clean REST API**: Simple endpoints to get the best Hacker News stories
- **Smart Architecture**: Proper separation between business logic, data access, and API layers
- **JWT Authentication**: Secure login system with token-based access
- **Caching**: Smart memory caching to avoid hitting the Hacker News API too often
- **Solid Testing**: Good test coverage with unit and integration tests
- **Docker Support**: Ready to containerize and deploy anywhere

## Architecture

The solution follows Domain-Driven Design principles with the following layers:

### Domain Layer (`HackerNewsAPI.Domain`)
- **Entities**: Core domain entities like `Story`
- **Value Objects**: Data transfer objects like `StoryDto`
- **Interfaces**: Repository interfaces defining contracts

### Application Layer (`HackerNewsAPI.Application`)
- **Services**: Business logic services like `StoryService`
- **Interfaces**: Application service interfaces

### Infrastructure Layer (`HackerNewsAPI.Infrastructure`)
- **Repositories**: Concrete implementations of repositories
- **External API Integration**: HTTP client for Hacker News API

### API Layer (`HackerNewsAPI`)
- **Controllers**: REST API endpoints
- **Configuration**: Dependency injection and middleware setup

## How to Use the API

### 🔐 Authentication First!

Before you can get stories, you need to log in to get a JWT token. The stories endpoint is protected and requires authentication.

**Test Credentials (for demo purposes only):**
- **Username**: `admin`
- **Password**: `Test123!`

Or you can use:
- **Username**: `user` 
- **Password**: `Test123!`

> **⚠️ Important Security Note**: These test credentials are hardcoded in the database for demonstration purposes only. In a real production application, you would never expose user credentials like this. This is just to show how JWT authentication works!

### Step 1: Login to Get Your Token

```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Test123!"}'
```

You'll get back something like:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin", 
  "role": "Admin",
  "expiresIn": 3600
}
```

### Step 2: Use Your Token to Get Stories

Now use that token to access the protected stories endpoint:

```bash
curl -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  "http://localhost:5000/api/stories/best?count=5"
```

### Stories Endpoint

**GET `/api/stories/best`**

Gets the top stories from Hacker News, sorted by score (highest first).

**Parameters:**
- `count` (optional): How many stories you want (default: 10, max: 100)

**Example Response:**
```json
[
  {
    "title": "Ghostty is leaving GitHub",
    "uri": "https://mitchellh.com/writing/ghostty-leaving-github", 
    "postedBy": "WadeGrimridge",
    "time": "2026-04-28T19:44:52Z",
    "score": 3423,
    "commentCount": 1023
  },
  {
    "title": "Zed 1.0",
    "uri": "https://zed.dev/blog/zed-1-0",
    "postedBy": "salkahfi", 
    "time": "2026-04-29T14:34:19Z",
    "score": 1967,
    "commentCount": 629
  }
]
```

## Getting Started

### What You'll Need
- .NET 9.0 SDK
- Your favorite code editor (VS Code, Visual Studio, etc.)

### Let's Get It Running!

1. **Clone the repo:**
```bash
git clone <repository-url>
cd HackerNewsAPI
```

2. **Install the packages:**
```bash
dotnet restore
```

3. **Build it:**
```bash
dotnet build
```

4. **Fire it up!**
```bash
cd HackerNewsAPI
dotnet run
```

The API will be running at `https://localhost:5001` or `http://localhost:5000`.

🎉 **Quick Test**: If you see stories coming back after following the authentication steps above, you're all set!

## Docker Support (Optional)

Want to run this in a container? Easy! 

### What You'll Need
- Docker Desktop installed
- Docker Compose (comes with Docker Desktop)

### Quick Start with Docker Compose

1. **Build and run everything:**
```bash
docker-compose up --build
```

2. **Run it in the background:**
```bash
docker-compose up -d --build
```

3. **Stop it when you're done:**
```bash
docker-compose down
```

4. **See what's happening:**
```bash
docker-compose logs -f hackernewsapi
```

The API will be available at `http://localhost:8080` when using Docker.

### Docker Without Compose

If you prefer plain Docker:

1. **Build the image:**
```bash
docker build -t hackernews-api .
```

2. **Run it:**
```bash
docker run -p 8080:8080 --name hackernews-api hackernews-api
```

3. **Run in background:**
```bash
docker run -d -p 8080:8080 --name hackernews-api hackernews-api
```

4. **Clean up:**
```bash
docker stop hackernews-api
docker rm hackernews-api
```

### What's Inside the Docker Setup?

- **Smart multi-stage builds**: Keeps the final image small and fast
- **Runs as non-root user**: Better security practices
- **Configurable settings**: Easy to change through docker-compose
- **Persistent logs**: Your logs won't disappear when the container restarts

### Production Docker Setup

For production-like setup with nginx reverse proxy:

```bash
docker-compose --profile production up -d
```

This will start both the API and nginx reverse proxy on ports 80 and 443.

## Testing the Code

Want to make sure everything works as expected? Run the tests!

### Run All Tests
```bash
dotnet test
```

### Run Tests for a Specific Project
```bash
dotnet test HackerNewsAPI.UnitTests
dotnet test HackerNewsAPI.IntegrationTests
```

### What the Tests Cover
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test how different parts work together
- **Authentication Tests**: Make sure login and JWT tokens work
- **API Tests**: Verify the endpoints return the right data

## Quick Examples

### Using the API with curl

```bash
# 1. Login to get a token
TOKEN=$(curl -s -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Test123!"}' | \
  grep -o '"token":"[^"]*' | cut -d'"' -f4)

# 2. Use the token to get stories (various counts)
curl -H "Authorization: Bearer $TOKEN" "http://localhost:5000/api/stories/best?count=5"
curl -H "Authorization: Bearer $TOKEN" "http://localhost:5000/api/stories/best"        # default 10
curl -H "Authorization: Bearer $TOKEN" "http://localhost:5000/api/stories/best?count=100" # max 100
```

### Using with Postman or Insomnia

1. **Create a POST request** to `http://localhost:5000/api/auth/login`
2. **Body**: Raw JSON with `{"username":"admin","password":"Test123!"}`
3. **Copy the token** from the response
4. **Create a GET request** to `http://localhost:5000/api/stories/best`
5. **Add Authorization header**: `Bearer YOUR_TOKEN_HERE`

### Common Issues & Solutions

**401 Unauthorized?**
- Make sure you're using a valid token
- Check that the token hasn't expired (1 hour)
- Verify your Authorization header format: `Bearer token`

**404 Not Found?**
- Check that you're using the right URL
- Make sure the API is running on the correct port

**500 Internal Server Error?**
- Check the application logs
- Make sure the Hacker News API is accessible
- Try running the API locally first to debug

## Configuration

The application can be configured through `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Caching Strategy

To prevent overloading the Hacker News API, the application implements:

- **Memory Caching**: Stories and story IDs are cached for 5 minutes
- **Cache Keys**: 
  - `best_stories` for the list of best story IDs
  - `story_{id}` for individual story details
- **Cache Duration**: 5 minutes for all cached data

## Assumptions

1. **API Availability**: The Hacker News API is assumed to be available and stable
2. **Data Format**: The JSON structure from Hacker News API is assumed to remain consistent
3. **Rate Limiting**: The 5-minute cache duration is assumed to be sufficient to avoid rate limiting
4. **Story Validity**: All returned story IDs from the best stories endpoint are assumed to be valid stories

## Enhancements and Future Improvements

Given more time, the following enhancements could be implemented:

### Performance & Scalability
1. **Distributed Caching**: Replace memory cache with Redis for better scalability
2. **Background Refresh**: Implement background jobs to refresh cache proactively
3. **Connection Pooling**: Optimize HTTP client usage with connection pooling
4. **Async Processing**: Implement streaming responses for large datasets

### Resilience & Reliability
1. **Circuit Breaker**: Implement circuit breaker pattern for external API calls
2. **Retry Policies**: Add exponential backoff retry mechanisms
3. **Health Checks**: Add comprehensive health check endpoints
4. **Graceful Degradation**: Return cached data when API is unavailable

### Features
1. **Pagination**: Implement pagination for large result sets
2. **Filtering**: Add filtering options (by score range, date range, etc.)
3. **Search**: Implement full-text search capabilities
4. **Real-time Updates**: Add WebSocket support for real-time story updates
5. **Analytics**: Track usage patterns and popular stories

### Monitoring & Observability
1. **Application Insights**: Integrate with Azure Application Insights
2. **Custom Metrics**: Add custom metrics for API performance
3. **Structured Logging**: Implement structured logging with correlation IDs
4. **Distributed Tracing**: Add distributed tracing for microservices

### Security
1. **API Key Management**: Add API key authentication
2. **Rate Limiting**: Implement per-client rate limiting
3. **Input Validation**: Enhanced input validation and sanitization
4. **CORS Configuration**: Proper CORS configuration for production

### Testing
1. **Integration Tests**: Add more comprehensive integration tests
2. **Load Testing**: Implement load testing scenarios
3. **Contract Testing**: Add contract tests for external API integration
4. **Performance Tests**: Add performance benchmarking tests

## Technology Stack

- **.NET 9.0**: Latest .NET framework
- **ASP.NET Core**: Web framework
- **xUnit**: Testing framework
- **Moq**: Mocking framework for unit tests
- **Microsoft.Extensions.Caching.Memory**: In-memory caching
- **Microsoft.Extensions.Logging**: Logging framework
- **System.Text.Json**: JSON serialization

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License

This project is licensed under the MIT License.
