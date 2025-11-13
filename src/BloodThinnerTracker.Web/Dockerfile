## Multi-stage Dockerfile for BloodThinnerTracker.Web
## Build with .NET 10 SDK, run on lightweight Alpine-based ASP.NET runtime

# Stage 1 - build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the repository and restore/publish the Web project
COPY . ./

# Restore and publish the Web project
RUN dotnet restore src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj
RUN dotnet publish src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj -c Release -o /app --no-restore

# Stage 2 - runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Create a non-root user for better security
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

WORKDIR /app

# Copy published output from build stage
COPY --from=build /app /app

# Copy entrypoint script into image and make executable
COPY docs/docker/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod 0555 /docker-entrypoint.sh

# Ensure the non-root user owns the app files
USER appuser

# Configure listening port used by the app
ENV ASPNETCORE_URLS=http://+:5235
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose web port
EXPOSE 5235

# Start the app via the entrypoint script which exports secrets then execs the app
ENTRYPOINT ["/docker-entrypoint.sh", "dotnet", "BloodThinnerTracker.Web.dll"]

