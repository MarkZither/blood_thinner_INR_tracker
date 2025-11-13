## Multi-stage Dockerfile for BloodThinnerTracker.Api (CLEAN COPY)
## Build with .NET 10 SDK, run on lightweight Alpine-based ASP.NET runtime

# Stage 1 - build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy repository and restore/publish API project
COPY . ./
RUN dotnet restore src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj
RUN dotnet publish src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj -c Release -o /app --no-restore

# Stage 2 - runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Create a non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

WORKDIR /app

# Copy published output
COPY --from=build /app /app

# Copy entrypoint script into image and make executable
COPY docs/docker/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod 0555 /docker-entrypoint.sh

# Set permissions
RUN chown -R appuser:appgroup /app

USER appuser

# API listens on port 5234 by default
ENV ASPNETCORE_URLS=http://+:5234
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5234

ENTRYPOINT ["/docker-entrypoint.sh", "dotnet", "BloodThinnerTracker.Api.dll"]
