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

# Stage 2 - Runtime with Noble Chiseled + Azure CLI
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install Azure CLI via official script and a small privilege-drop helper (gosu)
USER root
RUN apt-get update && apt-get install -y curl ca-certificates gnupg gosu \
    && curl -sL https://aka.ms/InstallAzureCLIDeb | bash \
    && apt-get clean

RUN az version

# Create non-root user and group
RUN groupadd --system appgroup \
    && useradd --system --gid appgroup --create-home --shell /bin/bash appuser

WORKDIR /app

# Copy published output from build stage
COPY --from=build /app /app

# Copy entrypoint script into image and make executable
COPY docs/docker/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod 0555 /docker-entrypoint.sh

# Set permissions (keep files owned by appuser, but leave runtime user as root
# so the entrypoint can read root-owned secret files). The entrypoint will
# drop privileges to `appuser` before exec'ing the app.
RUN chown -R appuser:appgroup /app

# Configure listening port used by the app
ENV ASPNETCORE_URLS=http://+:5235
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose web port
EXPOSE 5235

# Start the app via the entrypoint script which exports secrets then execs the app
ENTRYPOINT ["/docker-entrypoint.sh", "dotnet", "BloodThinnerTracker.Web.dll"]

