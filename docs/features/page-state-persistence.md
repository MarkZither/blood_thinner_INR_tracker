# Page State Persistence

## Overview

The Page State Persistence feature allows Blazor pages to remember their state (pagination, filters, scroll position) when users navigate away and return. This improves user experience by maintaining context across navigation.

## Architecture

### Components

1. **IPageStateService** - Interface for state management operations
2. **PageStateService** - Implementation using `ProtectedSessionStorage`
3. **PageState** - Model for storing page state data

### Design Decisions

- **Session Storage**: Uses `ProtectedSessionStorage` for server-side Blazor (InteractiveServer mode)
- **Scope**: Service registered as Scoped to align with Blazor component lifecycle
- **Error Handling**: Non-critical errors are logged but don't interrupt user flow
- **State Key**: Each page uses a unique key (e.g., "inr-list") to identify its state

## Usage

### 1. Inject the Service

```csharp
@inject IPageStateService PageStateService
```

### 2. Define State Model

```csharp
private const string PageStateKey = "your-page-key";
private MudTable<YourType>? _table;
private PageState? _savedState;
```

### 3. Restore State on Initialization

```csharp
protected override async Task OnInitializedAsync()
{
    await RestorePageState();
    await LoadData();
}

private async Task RestorePageState()
{
    try
    {
        _savedState = await PageStateService.LoadStateAsync<PageState>(PageStateKey);
    }
    catch (Exception ex)
    {
        // Log error but continue with defaults
        Console.WriteLine($"Error restoring page state: {ex.Message}");
    }
}
```

### 4. Apply State After Render

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && _table != null && _savedState != null && !_stateRestored)
    {
        _table.RowsPerPage = _savedState.PageSize;
        _table.CurrentPage = _savedState.CurrentPage - 1; // MudTable uses 0-based index
        _stateRestored = true;
        StateHasChanged();
    }
}
```

### 5. Save State Before Navigation

```csharp
private async Task SavePageState()
{
    try
    {
        if (_table == null) return;

        var state = new PageState
        {
            PageSize = _table.RowsPerPage,
            CurrentPage = _table.CurrentPage + 1 // Save as 1-based index
        };
        await PageStateService.SaveStateAsync(PageStateKey, state);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error saving page state: {ex.Message}");
    }
}

private async Task NavigateToEdit(Guid id)
{
    await SavePageState();
    Navigation.NavigateTo($"/edit/{id}");
}
```

## PageState Model

The `PageState` model supports the following properties:

```csharp
public class PageState
{
    public int PageSize { get; set; } = 10;
    public int CurrentPage { get; set; } = 1;
    public int ScrollPosition { get; set; }
    public Dictionary<string, string>? AdditionalState { get; set; }
}
```

### Additional State Examples

Store filters, sort options, or other page-specific state:

```csharp
var state = new PageState
{
    PageSize = 25,
    CurrentPage = 2,
    AdditionalState = new Dictionary<string, string>
    {
        { "searchTerm", "warfarin" },
        { "statusFilter", "active" },
        { "sortBy", "name" },
        { "sortDirection", "asc" }
    }
};
```

## Example: INR Tracking Page

The INR list page demonstrates full implementation:

1. **State Persistence**: Saves page size and current page when navigating to edit/add
2. **State Restoration**: Restores table state when returning to the list
3. **MudTable Integration**: Binds to MudTable's RowsPerPage and CurrentPage properties

See `/src/BloodThinnerTracker.Web/Components/Pages/INRTracking.razor` for complete example.

## Testing

### Unit Tests

Tests validate the PageState model and service contract:

```csharp
[Fact]
public void PageState_DefaultValues_AreCorrect()
{
    var state = new PageState();
    Assert.Equal(10, state.PageSize);
    Assert.Equal(1, state.CurrentPage);
}
```

### Component Tests

When testing components that use `IPageStateService`, mock the service:

```csharp
var pageStateServiceMock = new Mock<IPageStateService>();
Services.AddSingleton(pageStateServiceMock.Object);
```

## Browser Compatibility

- **Server-Side Blazor**: Full support via `ProtectedSessionStorage`
- **WebAssembly Blazor**: Can be adapted to use `ProtectedLocalStorage`
- **Session Persistence**: State persists for the browser session duration
- **Cross-Tab**: State is tab-specific (session storage)

## Security Considerations

1. **Encryption**: `ProtectedSessionStorage` encrypts data before storing
2. **Scope**: Session storage is isolated to the current browser tab
3. **Validation**: Always validate restored state before applying
4. **No Sensitive Data**: Don't store sensitive user data in page state

## Future Enhancements

Potential improvements for the feature:

1. **Local Storage Option**: Add support for `ProtectedLocalStorage` for persistent state across sessions
2. **Scroll Position**: Implement scroll restoration using JavaScript interop
3. **State Expiration**: Add TTL for stale state cleanup
4. **State Migration**: Handle schema changes in PageState model
5. **State Sync**: Optionally sync state across browser tabs
