# ============================
# Build stage
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for dependency resolution
COPY Wordfeud.Api/*.csproj Wordfeud.Api/
COPY Wordfeud.Api.Tests/*.csproj Wordfeud.Api.Tests/
COPY Wordfeud.Api.IntegrationTests/*.csproj Wordfeud.Api.IntegrationTests/

# Restore dependencies
RUN dotnet restore Wordfeud.Api/Wordfeud.Api.csproj

# Copy source code
COPY Wordfeud.Api/ Wordfeud.Api/
COPY Wordfeud.Api.Tests/ Wordfeud.Api.Tests/
COPY Wordfeud.Api.IntegrationTests/ Wordfeud.Api.IntegrationTests/

# Build
WORKDIR /src/Wordfeud.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# ============================
# Runtime stage
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Wordfeud.Api.dll"]
