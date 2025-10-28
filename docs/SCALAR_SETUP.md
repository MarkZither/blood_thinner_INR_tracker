# Scalar API Documentation Setup

## Summary

Successfully added **Scalar API documentation** to the BloodThinnerTracker.Api project and configured VS Code for efficient development.

## Changes Made

### 1. **Added Scalar Package**
- **Package**: `Scalar.AspNetCore` v1.2.42
- **Location**: `src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj`

### 2. **Configured Scalar in Program.cs**
```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Blood Thinner Tracker API")
        .WithTheme(ScalarTheme.Mars)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithPreferredScheme("Bearer")
        .WithApiKeyAuthentication(x => x.Token = "your-api-key");
});
```

### 3. **Set Scalar as Default Page**
- Root path (`/`) now redirects to `/scalar/v1`
- Legacy info endpoint moved to `/info`
- Scalar documentation opens automatically when running the API

### 4. **Created VS Code Configuration**

#### `.vscode/launch.json`
Launch configurations for:
- **API (Scalar Docs)**: Runs API and opens Scalar docs automatically
- **Web (Blazor)**: Runs the Blazor web application
- **Aspire AppHost**: Runs the .NET Aspire orchestrator
- **API + Web**: Runs API standalone
- **Full Stack** (compound): Runs both API and Web together

#### `.vscode/tasks.json`
Build tasks for:
- `build-api`: Build API project
- `build-web`: Build Web project
- `build-apphost`: Build Aspire AppHost
- `build-all`: Build entire solution (default)
- `watch-api`: Hot reload for API
- `watch-web`: Hot reload for Web
- `clean`: Clean solution
- `test`: Run all tests (default test task)
- `restore`: Restore NuGet packages

## Usage

### Running the API with Scalar

1. **Using F5 (Debug)**:
   - Press `F5` or select "API (Scalar Docs)" from Run and Debug
   - Browser automatically opens to Scalar documentation

2. **Using Command Palette**:
   - `Ctrl+Shift+P` → "Tasks: Run Build Task"
   - Select the appropriate task

3. **Manual Terminal**:
   ```powershell
   dotnet run --project src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj
   ```
   - Navigate to: https://localhost:7000/scalar/v1

### Accessing Endpoints

- **Scalar API Docs**: https://localhost:7000/scalar/v1
- **OpenAPI JSON**: https://localhost:7000/openapi/v1.json
- **API Info**: https://localhost:7000/info
- **Health Check**: https://localhost:7000/health
- **Medical Disclaimer**: https://localhost:7000/disclaimer

### Keyboard Shortcuts

- `F5`: Start debugging with selected configuration
- `Ctrl+Shift+B`: Run default build task (build-all)
- `Ctrl+Shift+P`: Command Palette

## Scalar Features

- **Interactive API Documentation**: Try API endpoints directly from the browser
- **Request Builder**: Build and test API requests with authentication
- **Code Generation**: Generate C# HttpClient code for API calls
- **Schema Visualization**: View request/response models
- **Mars Theme**: Modern, medical-appropriate color scheme

## Next Steps

1. **Add XML Documentation**: Enhance API documentation with XML comments
2. **Configure Authentication**: Set up proper JWT Bearer tokens in Scalar
3. **Add Examples**: Include request/response examples in controllers
4. **Customize Theme**: Adjust Scalar theme colors for branding

## Notes

- Scalar only runs in Development environment
- Uses .NET 10 RC 2 with latest features
- Configured for medical application compliance
- Includes CORS settings for local development

## Troubleshooting

### Scalar not loading?
- Ensure you're running in Development environment
- Check that port 7000 (HTTPS) or 5000 (HTTP) is not in use
- Verify the OpenAPI document is generated at `/openapi/v1.json`

### Build errors?
- Run `dotnet restore` to ensure packages are restored
- Check that .NET 10 SDK RC 2 is installed: `dotnet --version`
- Clean and rebuild: `dotnet clean && dotnet build`
