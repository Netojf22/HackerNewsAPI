# Use the official .NET 9.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the .NET 9.0 SDK for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["HackerNewsAPI/HackerNewsAPI.csproj", "HackerNewsAPI/"]
COPY ["HackerNewsAPI.Domain/HackerNewsAPI.Domain.csproj", "HackerNewsAPI.Domain/"]
COPY ["HackerNewsAPI.Application/HackerNewsAPI.Application.csproj", "HackerNewsAPI.Application/"]
COPY ["HackerNewsAPI.Infrastructure/HackerNewsAPI.Infrastructure.csproj", "HackerNewsAPI.Infrastructure/"]
COPY ["HackerNewsAPI.UnitTests/HackerNewsAPI.UnitTests.csproj", "HackerNewsAPI.UnitTests/"]
COPY ["HackerNewsAPI.IntegrationTests/HackerNewsAPI.IntegrationTests.csproj", "HackerNewsAPI.IntegrationTests/"]

RUN dotnet restore "./HackerNewsAPI/HackerNewsAPI.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/HackerNewsAPI"
RUN dotnet build "./HackerNewsAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HackerNewsAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Build the final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user for security
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --gid 1001 appuser

# Change ownership of the app directory to the non-root user
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "HackerNewsAPI.dll"]
