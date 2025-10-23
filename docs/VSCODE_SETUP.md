# VS Code Development Setup

## Quick Start

Press **F5** to launch the API with Scalar documentation!

## Available Launch Configurations

### 1. API (Scalar Docs) ⭐ **Recommended**
- Builds and runs the API project
- Automatically opens Scalar API documentation at `/scalar/v1`
- **URL**: https://localhost:7000/scalar/v1

### 2. Web (Blazor)
- Runs the Blazor Server web application
- **URL**: https://localhost:7001

### 3. Aspire AppHost
- Runs the .NET Aspire orchestration host
- Manages all services (API, Web, etc.)

### 4. Full Stack (Compound)
- Runs both API and Web simultaneously
- Best for full-stack development

## Available Tasks

### Build Tasks
- **Build All** (Ctrl+Shift+B): Default build task for entire solution
- **Build API**: Build only the API project
- **Build Web**: Build only the Web project
- **Build AppHost**: Build only the Aspire AppHost

### Development Tasks
- **Watch API**: Hot reload for API changes
- **Watch Web**: Hot reload for Web changes

### Maintenance Tasks
- **Clean**: Clean all build artifacts
- **Test**: Run all unit tests
- **Restore**: Restore NuGet packages

## Running from Terminal

### API with Scalar
```powershell
dotnet run --project .\src\BloodThinnerTracker.Api\BloodThinnerTracker.Api.csproj
```

### Web
```powershell
dotnet run --project .\src\BloodThinnerTracker.Web\BloodThinnerTracker.Web.csproj
```

### Aspire (All Services)
```powershell
dotnet run --project .\src\BloodThinnerTracker.AppHost\BloodThinnerTracker.AppHost.csproj
```

## Port Configuration

| Service | HTTPS | HTTP |
|---------|-------|------|
| API     | 7000  | 5000 |
| Web     | 7001  | 5001 |

## Features

✅ **Automatic Browser Launch**: Opens Scalar docs when API starts
✅ **Hot Reload**: Watch tasks enable live code updates
✅ **IntelliSense**: Full C# and Razor support
✅ **Debugging**: Set breakpoints and step through code
✅ **Problem Matching**: Compiler errors shown in Problems panel

## Scalar API Documentation

The API now uses **Scalar** for modern, interactive API documentation:

- 📚 **Interactive Docs**: Test endpoints directly in the browser
- 🎨 **Mars Theme**: Medical-appropriate color scheme
- 🔒 **Authentication**: JWT Bearer token support
- 📝 **Code Generation**: Generate C# HttpClient code
- 🔍 **Schema Browser**: Explore request/response models

## Workflow

1. **Start Development**
   - Press `F5` to launch API with Scalar
   - Or use compound "Full Stack" to run everything

2. **Make Changes**
   - Edit code in any project
   - Use watch tasks for hot reload

3. **Test API**
   - Use Scalar documentation to test endpoints
   - Or use the `.http` files with REST Client extension

4. **Debug**
   - Set breakpoints in code
   - Step through with F10/F11

5. **Build & Test**
   - `Ctrl+Shift+B` to build solution
   - Use Test Explorer or run `dotnet test`

## Tips

- **Multiple Services**: Use the "Full Stack" compound configuration
- **API Only**: Use "API (Scalar Docs)" for backend-only development
- **Fast Iteration**: Use watch tasks during active development
- **Clean Build**: Run "clean" task if you encounter build issues

## Troubleshooting

### Port Already in Use
```powershell
# Find process using port
netstat -ano | findstr :7000

# Kill process (replace PID)
taskkill /PID <pid> /F
```

### Package Restore Issues
```powershell
dotnet restore
dotnet clean
dotnet build
```

### Scalar Not Loading
- Ensure you're in Development mode
- Check `/openapi/v1.json` endpoint works
- Verify package `Scalar.AspNetCore` is installed

## Extensions Recommended

- **C# Dev Kit**: Enhanced C# support
- **REST Client**: Test `.http` files
- **.NET Install Tool**: Manage .NET SDKs
- **GitLens**: Enhanced Git integration

## Documentation

- [Scalar Setup](./SCALAR_SETUP.md) - Detailed Scalar configuration
- [API Documentation](./api/README.md) - API endpoints and examples
- [Deployment Guide](./deployment/README.md) - Production deployment

---

**Happy Coding! 🚀**
