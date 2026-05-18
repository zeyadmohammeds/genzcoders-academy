# ─── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (layer-cached)
COPY GenZCoders.csproj .
RUN dotnet restore GenZCoders.csproj

# Copy the rest of the source
COPY . .

# Publish a self-contained Release build
RUN dotnet publish GenZCoders.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# ─── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install timezone data & curl for health checks
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/*

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy published output
COPY --from=build /app/publish .

# Give ownership to the non-root user
RUN chown -R appuser:appgroup /app
USER appuser

# Monster sets PORT env var — ASP.NET reads ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose the port Monster will route traffic to
EXPOSE 8080

# Health-check so Monster knows the container is alive
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:${PORT:-8080}/health || exit 1

ENTRYPOINT ["dotnet", "GenZCoders.dll"]
