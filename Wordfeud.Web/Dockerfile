# ============================
# Build stage
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY WordfeudApi.sln .
COPY Wordfeud.Api/Wordfeud.Api.csproj Wordfeud.Api/
COPY Wordfeud.Web/Wordfeud.Web.csproj Wordfeud.Web/

# Restore dependencies
RUN dotnet restore Wordfeud.Web/Wordfeud.Web.csproj

# Copy source code
COPY Wordfeud.Api/ Wordfeud.Api/
COPY Wordfeud.Web/ Wordfeud.Web/

# Build and publish
WORKDIR /src/Wordfeud.Web
RUN dotnet publish -c Release -o /app/publish --no-restore /p:ErrorOnDuplicatePublishOutputFiles=false

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
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8081/ || exit 1

# Run the application
# ApiUrl can be overridden via docker-compose or docker run
ENV ApiUrl=http://wordfeud-api:8080
ENTRYPOINT ["dotnet", "Wordfeud.Web.dll"]
