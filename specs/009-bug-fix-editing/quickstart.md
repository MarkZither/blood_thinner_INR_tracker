# quickstart.md

Local quickstart to test INR edit/delete with audit interceptor

1. Ensure you are on feature branch: `009-bug-fix-editing`
2. Restore and build the solution:

```pwsh
dotnet restore
dotnet build
```

3. Start the API (in one terminal):

```pwsh
dotnet run --project src/BloodThinnerTracker.Api/BloodThinnerTracker.Api.csproj
```

4. Start the Web project (MudBlazor UI) in another terminal:

```pwsh
dotnet run --project src/BloodThinnerTracker.Web/BloodThinnerTracker.Web.csproj
```

5. Create a test user and add INR entries via the UI, then:
 - Edit an INR entry and verify the value updates and audit record created.
 - Delete (soft-delete) an INR entry and verify IsDeleted set and audit record created.

6. Run integration tests (if provided) or run the API integration tests suite:

```pwsh
dotnet test tests/BloodThinnerTracker.Api.Tests --filter "Category=Integration"
```
