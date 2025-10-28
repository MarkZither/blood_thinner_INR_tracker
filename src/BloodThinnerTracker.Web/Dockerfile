FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj", "src/BloodThinnerTracker.Web/"]
COPY ["src/BloodThinnerTracker.Shared/BloodThinnerTracker.Shared.csproj", "src/BloodThinnerTracker.Shared/"]
COPY ["src/BloodThinnerTracker.ServiceDefaults/BloodThinnerTracker.ServiceDefaults.csproj", "src/BloodThinnerTracker.ServiceDefaults/"]

RUN dotnet restore "src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj"

# Copy remaining source and publish
COPY . .
WORKDIR "/src/src/BloodThinnerTracker.Web"
RUN dotnet publish "BloodThinnerTracker.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BloodThinnerTracker.Web.dll"]
