# MudBlazor Migration Reference

**Feature**: 003-core-ui-foundation  
**Date**: October 29, 2025  
**Authority**: Constitution Principle III (User Experience Consistency & Pure .NET UI)

---

## Constitutional Mandate

> **Blazor Web applications MUST use MudBlazor component library for all UI components to maintain pure .NET implementation without JavaScript dependencies**. JavaScript interop MUST be avoided except where absolutely necessary for browser APIs (clipboard, file downloads). Charts, dialogs, data grids, and interactive components MUST use MudBlazor's native C# implementations.

---

## Quick Migration Checklist

### Remove These Frameworks ❌
- [ ] Bootstrap CSS/JS
- [ ] Font Awesome CSS
- [ ] jQuery (if present)
- [ ] Any other JavaScript UI libraries

### Use Only These ✅
- [x] MudBlazor (Material Design components)
- [x] Custom CSS for medical-specific styling only

---

## Component Mapping

### Layout & Navigation

| Bootstrap | MudBlazor | Icon Mapping |
|-----------|-----------|--------------|
| `<nav class="navbar">` | `<MudAppBar>` | - |
| `<div class="container">` | `<MudContainer>` | - |
| `<div class="row">` | `<MudGrid>` | - |
| `<div class="col-*">` | `<MudItem xs="*">` | - |
| Dropdown menu | `<MudMenu>` + `<MudMenuItem>` | - |
| Navbar toggler | `<MudDrawer>` with responsive | - |

### Forms

| Bootstrap | MudBlazor | Props |
|-----------|-----------|-------|
| `<input class="form-control">` | `<MudTextField>` | `T="string"` |
| `<input type="email">` | `<MudTextField>` | `InputType="InputType.Email"` |
| `<input type="password">` | `<MudTextField>` | `InputType="InputType.Password"` |
| `<input type="number">` | `<MudNumericField>` | `T="int"` or `T="decimal"` |
| `<input type="date">` | `<MudDatePicker>` | `T="DateTime?"` |
| `<select>` | `<MudSelect>` | `T="string"` |
| `<textarea>` | `<MudTextField>` | `Lines="3"` |
| `<input type="checkbox">` | `<MudCheckBox>` | `T="bool"` |
| Toggle switch | `<MudSwitch>` | `T="bool"` |
| `<label>` | `Label="Text"` attribute | On MudBlazor component |

### Buttons

| Bootstrap | MudBlazor | Variant |
|-----------|-----------|---------|
| `<button class="btn btn-primary">` | `<MudButton Color="Color.Primary">` | `Variant="Filled"` |
| `<button class="btn btn-outline-primary">` | `<MudButton Color="Color.Primary">` | `Variant="Outlined"` |
| `<button class="btn btn-link">` | `<MudButton Color="Color.Primary">` | `Variant="Text"` |
| `<button class="btn btn-success">` | `<MudButton Color="Color.Success">` | `Variant="Filled"` |
| `<button class="btn btn-danger">` | `<MudButton Color="Color.Error">` | `Variant="Filled"` |
| Icon button | `<MudIconButton Icon="@Icons.*">` | - |
| FAB (floating) | `<MudFab Color="Color.Primary">` | - |

### Data Display

| Bootstrap | MudBlazor | Features |
|-----------|-----------|----------|
| `<table class="table">` | `<MudTable>` | Basic table |
| `<table class="table table-striped">` | `<MudDataGrid>` | Sortable, filterable, paginated |
| `<div class="card">` | `<MudCard>` | Container |
| Card header | `<MudCardHeader>` | - |
| Card body | `<MudCardContent>` | - |
| Card footer | `<MudCardActions>` | - |
| `<div class="badge">` | `<MudChip>` | Colored labels |
| List group | `<MudList>` + `<MudListItem>` | - |

### Feedback

| Bootstrap | MudBlazor | Severity |
|-----------|-----------|----------|
| `<div class="alert alert-success">` | `<MudAlert Severity="Severity.Success">` | Green |
| `<div class="alert alert-warning">` | `<MudAlert Severity="Severity.Warning">` | Orange |
| `<div class="alert alert-danger">` | `<MudAlert Severity="Severity.Error">` | Red |
| `<div class="alert alert-info">` | `<MudAlert Severity="Severity.Info">` | Blue |
| Toast/Snackbar | `ISnackbar.Add()` | Injected service |
| Modal | `<MudDialog>` | Via `IDialogService` |
| Spinner | `<MudProgressCircular>` | - |
| Progress bar | `<MudProgressLinear>` | - |

### Icons

| Font Awesome | MudBlazor Material Icons | Example |
|--------------|--------------------------|---------|
| `<i class="fas fa-home">` | `<MudIcon Icon="@Icons.Material.Filled.Home" />` | Home |
| `<i class="fas fa-dashboard">` | `<MudIcon Icon="@Icons.Material.Filled.Dashboard" />` | Dashboard |
| `<i class="fas fa-pills">` | `<MudIcon Icon="@Icons.Material.Filled.Medication" />` | Medications |
| `<i class="fas fa-chart-line">` | `<MudIcon Icon="@Icons.Material.Filled.ShowChart" />` | Charts |
| `<i class="fas fa-user">` | `<MudIcon Icon="@Icons.Material.Filled.Person" />` | User |
| `<i class="fas fa-cog">` | `<MudIcon Icon="@Icons.Material.Filled.Settings" />` | Settings |
| `<i class="fas fa-sign-out-alt">` | `<MudIcon Icon="@Icons.Material.Filled.Logout" />` | Logout |
| `<i class="fas fa-plus">` | `<MudIcon Icon="@Icons.Material.Filled.Add" />` | Add |
| `<i class="fas fa-edit">` | `<MudIcon Icon="@Icons.Material.Filled.Edit" />` | Edit |
| `<i class="fas fa-trash">` | `<MudIcon Icon="@Icons.Material.Filled.Delete" />` | Delete |
| `<i class="fas fa-search">` | `<MudIcon Icon="@Icons.Material.Filled.Search" />` | Search |
| `<i class="fas fa-calendar">` | `<MudIcon Icon="@Icons.Material.Filled.CalendarToday" />` | Calendar |
| `<i class="fas fa-check">` | `<MudIcon Icon="@Icons.Material.Filled.Check" />` | Check |
| `<i class="fas fa-times">` | `<MudIcon Icon="@Icons.Material.Filled.Close" />` | Close |
| `<i class="fas fa-exclamation-triangle">` | `<MudIcon Icon="@Icons.Material.Filled.Warning" />` | Warning |
| `<i class="fas fa-info-circle">` | `<MudIcon Icon="@Icons.Material.Filled.Info" />` | Info |

**Browse all icons**: https://mudblazor.com/features/icons

---

## Layout Example

### Before (Bootstrap) ❌
```razor
<nav class="navbar navbar-expand-lg navbar-dark bg-primary">
    <div class="container-fluid">
        <a class="navbar-brand" href="/">
            <i class="fas fa-heartbeat"></i> Blood Thinner Tracker
        </a>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse">
            <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse">
            <ul class="navbar-nav">
                <li class="nav-item">
                    <a class="nav-link" href="/dashboard">
                        <i class="fas fa-tachometer-alt"></i> Dashboard
                    </a>
                </li>
            </ul>
        </div>
    </div>
</nav>
```

### After (MudBlazor) ✅
```razor
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" 
                       Color="Color.Inherit" 
                       Edge="Edge.Start" 
                       OnClick="@ToggleDrawer" />
        <MudIcon Icon="@Icons.Material.Filled.FavoriteBorder" />
        <MudText Typo="Typo.h6" Class="ml-3">Blood Thinner Tracker</MudText>
        <MudSpacer />
        <MudIconButton Icon="@Icons.Material.Filled.Logout" 
                       Color="Color.Inherit" 
                       OnClick="@Logout" />
    </MudAppBar>
    
    <MudDrawer @bind-Open="@_drawerOpen" Elevation="1">
        <MudNavMenu>
            <MudNavLink Href="/dashboard" 
                        Icon="@Icons.Material.Filled.Dashboard" 
                        Match="NavLinkMatch.All">Dashboard</MudNavLink>
            <MudNavLink Href="/medications" 
                        Icon="@Icons.Material.Filled.Medication">Medications</MudNavLink>
            <MudNavLink Href="/inr" 
                        Icon="@Icons.Material.Filled.ShowChart">INR Tracking</MudNavLink>
        </MudNavMenu>
    </MudDrawer>
    
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.Large" Class="my-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;
}
```

---

## Form Example

### Before (Bootstrap) ❌
```razor
<div class="mb-3">
    <label class="form-label">Email</label>
    <input type="email" class="form-control" @bind="email" />
    @if (!string.IsNullOrEmpty(emailError))
    {
        <div class="text-danger">@emailError</div>
    }
</div>
<button type="submit" class="btn btn-primary">Submit</button>
```

### After (MudBlazor) ✅
```razor
<MudTextField T="string" 
              Label="Email" 
              @bind-Value="email" 
              InputType="InputType.Email"
              Variant="Variant.Outlined"
              Required="true"
              RequiredError="Email is required"
              Error="@(!string.IsNullOrEmpty(emailError))"
              ErrorText="@emailError" />

<MudButton Variant="Variant.Filled" 
           Color="Color.Primary" 
           ButtonType="ButtonType.Submit">Submit</MudButton>
```

---

## Common Patterns

### Loading State
```razor
@if (loading)
{
    <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
}
else
{
    <!-- Your content -->
}
```

### Error Alert
```razor
@if (!string.IsNullOrEmpty(errorMessage))
{
    <MudAlert Severity="Severity.Error" 
              ContentAlignment="HorizontalAlignment.Center"
              CloseIconClicked="@(() => errorMessage = null)">
        @errorMessage
    </MudAlert>
}
```

### Empty State
```razor
@if (!items.Any())
{
    <MudPaper Class="pa-6 ma-4" Elevation="0">
        <MudText Typo="Typo.h6" Align="Align.Center">
            <MudIcon Icon="@Icons.Material.Filled.Inbox" Size="Size.Large" />
        </MudText>
        <MudText Typo="Typo.body1" Align="Align.Center">No items found</MudText>
        <MudButton Color="Color.Primary" 
                   Variant="Variant.Filled" 
                   OnClick="@AddItem"
                   Class="mt-2">Add Your First Item</MudButton>
    </MudPaper>
}
```

---

## Testing Checklist

After migration, verify:

- [ ] No Bootstrap CSS files in `wwwroot/css/`
- [ ] No Font Awesome CSS files or CDN links
- [ ] No `bootstrap.bundle.min.js` references
- [ ] All `<i class="fas fa-*">` replaced with `<MudIcon>`
- [ ] All `class="btn"` replaced with `<MudButton>`
- [ ] All `class="form-control"` replaced with MudBlazor forms
- [ ] All `class="card"` replaced with `<MudCard>`
- [ ] All `class="alert"` replaced with `<MudAlert>`
- [ ] All `class="table"` replaced with `<MudTable>` or `<MudDataGrid>`
- [ ] Layout uses `<MudLayout>`, `<MudAppBar>`, `<MudDrawer>`
- [ ] Navigation uses `<MudNavMenu>` and `<MudNavLink>`
- [ ] Project builds without Bootstrap/Font Awesome warnings
- [ ] All pages render correctly at 320px, 768px, 1024px, 1440px
- [ ] No console errors related to missing CSS

---

## Resources

- **MudBlazor Documentation**: https://mudblazor.com/
- **Component Gallery**: https://mudblazor.com/components/
- **Icons Browser**: https://mudblazor.com/features/icons
- **Themes**: https://mudblazor.com/customization/default-theme
- **Examples**: https://mudblazor.com/getting-started/examples

---

**Remember**: The goal is **100% MudBlazor, 0% Bootstrap/Font Awesome**. If you find yourself writing `class="btn"` or `<i class="fas">`, stop and use the MudBlazor equivalent instead.
