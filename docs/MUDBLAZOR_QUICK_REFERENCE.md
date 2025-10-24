# MudBlazor Quick Reference for Blood Thinner Tracker

## Installation & Setup

### 1. Package Installation (Already Done)
```bash
dotnet add package MudBlazor
```

### 2. Service Registration (Program.cs)
```csharp
using MudBlazor.Services;

builder.Services.AddMudServices();
```

### 3. Add to App.razor
```html
<!-- In <head> -->
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />

<!-- Before </body> -->
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

### 4. Add Providers to MainLayout.razor
```html
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

### 5. Add Global Using (_Imports.razor)
```csharp
@using MudBlazor
```

---

## Common Components

### Charts (INR Trends, Medication History)

```csharp
@code {
    private List<ChartSeries> chartSeries = new();
    private string[] chartLabels = Array.Empty<string>();
    private ChartOptions chartOptions = new ChartOptions();

    private void PrepareChartData()
    {
        // X-axis labels (dates)
        chartLabels = inrTests.Select(t => t.TestDate.ToString("MMM dd")).ToArray();
        
        // Y-axis data
        var inrValues = inrTests.Select(t => (double)t.INRValue).ToArray();
        var targetMin = Enumerable.Repeat(2.0, inrTests.Count).ToArray();
        var targetMax = Enumerable.Repeat(3.0, inrTests.Count).ToArray();

        chartSeries = new List<ChartSeries>
        {
            new ChartSeries { Name = "INR Value", Data = inrValues },
            new ChartSeries { Name = "Target Min", Data = targetMin },
            new ChartSeries { Name = "Target Max", Data = targetMax }
        };

        chartOptions = new ChartOptions
        {
            YAxisTicks = 1,
            YAxisLines = true,
            XAxisLines = true
        };
    }
}
```

```html
<MudChart ChartType="ChartType.Line" 
          ChartSeries="@chartSeries" 
          XAxisLabels="@chartLabels" 
          Width="100%" 
          Height="400px"
          ChartOptions="@chartOptions">
</MudChart>
```

### Dialogs (Delete Confirmation)

```csharp
@inject IDialogService DialogService

private async Task DeleteINRTest(string testId)
{
    var parameters = new DialogParameters
    {
        ["Content"] = "Are you sure you want to delete this INR test result? This action cannot be undone."
    };

    var dialog = await DialogService.ShowMessageBox(
        "Confirm Deletion",
        "Are you sure you want to delete this INR test result? This action cannot be undone.",
        yesText: "Delete", 
        cancelText: "Cancel"
    );

    if (dialog == true)
    {
        // Delete logic
        await DeleteTestFromApi(testId);
        Snackbar.Add("INR test deleted successfully", Severity.Success);
    }
}
```

### Snackbar Notifications (Success/Error Messages)

```csharp
@inject ISnackbar Snackbar

// Success notification
Snackbar.Add("INR test saved successfully!", Severity.Success);

// Error notification
Snackbar.Add("Failed to save INR test. Please try again.", Severity.Error);

// Warning notification
Snackbar.Add("This INR value is outside your target range", Severity.Warning);

// Info notification
Snackbar.Add("Data syncing across devices...", Severity.Info);

// Custom duration (default is 3 seconds)
Snackbar.Add("Important message", Severity.Normal, config =>
{
    config.ShowCloseIcon = true;
    config.VisibleStateDuration = 5000; // 5 seconds
});
```

### Data Tables (Medication Log, INR History)

```html
<MudDataGrid T="INRTest" Items="@inrTests" Filterable="true" SortMode="SortMode.Multiple">
    <Columns>
        <PropertyColumn Property="x => x.TestDate" Title="Date" Format="MMM dd, yyyy" />
        <PropertyColumn Property="x => x.INRValue" Title="INR Value" Format="F1" />
        <PropertyColumn Property="x => x.Laboratory" Title="Laboratory" />
        <PropertyColumn Property="x => x.Notes" Title="Notes" />
        <TemplateColumn Title="Actions">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Edit" 
                               Size="Size.Small" 
                               OnClick="@(() => EditTest(context.Item.Id))" />
                <MudIconButton Icon="@Icons.Material.Filled.Delete" 
                               Size="Size.Small" 
                               Color="Color.Error"
                               OnClick="@(() => DeleteTest(context.Item.Id))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
    <PagerContent>
        <MudDataGridPager T="INRTest" />
    </PagerContent>
</MudDataGrid>
```

### Form Inputs

```html
<!-- Text Input -->
<MudTextField @bind-Value="laboratory" 
              Label="Laboratory" 
              Variant="Variant.Outlined" 
              Required="true" />

<!-- Numeric Input (for INR values) -->
<MudNumericField @bind-Value="inrValue" 
                 Label="INR Value" 
                 Variant="Variant.Outlined"
                 Min="0.5m" 
                 Max="8.0m" 
                 Step="0.1m"
                 Required="true" />

<!-- Date Picker -->
<MudDatePicker @bind-Date="testDate" 
               Label="Test Date" 
               Variant="Variant.Outlined"
               Required="true" />

<!-- Select Dropdown -->
<MudSelect @bind-Value="selectedFrequency" 
           Label="Test Frequency" 
           Variant="Variant.Outlined">
    <MudSelectItem Value="@("Weekly")">Weekly</MudSelectItem>
    <MudSelectItem Value="@("Bi-weekly")">Bi-weekly</MudSelectItem>
    <MudSelectItem Value="@("Monthly")">Monthly</MudSelectItem>
</MudSelect>

<!-- Autocomplete (for medication names) -->
<MudAutocomplete T="string" 
                 @bind-Value="medicationName"
                 Label="Medication" 
                 SearchFunc="@SearchMedications"
                 Variant="Variant.Outlined" />
```

### Cards & Layout

```html
<!-- Statistics Card -->
<MudCard Elevation="2">
    <MudCardContent>
        <div class="d-flex align-center justify-center mb-2">
            <MudIcon Icon="@Icons.Material.Filled.TrendingUp" 
                     Color="Color.Success" 
                     Size="Size.Large" />
        </div>
        <MudText Typo="Typo.h4" Align="Align.Center">@currentINR</MudText>
        <MudText Typo="Typo.body2" Align="Align.Center" Color="Color.Default">
            Current INR
        </MudText>
        <MudText Typo="Typo.caption" Align="Align.Center" Color="Color.Default">
            @currentINRDate?.ToString("MMM dd, yyyy")
        </MudText>
    </MudCardContent>
</MudCard>

<!-- Paper Container -->
<MudPaper Class="pa-4 mb-4" Elevation="2">
    <MudText Typo="Typo.h5" Class="mb-4">
        <MudIcon Icon="@Icons.Material.Filled.ShowChart" Class="mr-2" />
        INR Trend Analysis
    </MudText>
    <!-- Chart content -->
</MudPaper>

<!-- Grid Layout -->
<MudGrid>
    <MudItem xs="12" sm="6" md="3">
        <!-- Stat card 1 -->
    </MudItem>
    <MudItem xs="12" sm="6" md="3">
        <!-- Stat card 2 -->
    </MudItem>
    <MudItem xs="12" sm="6" md="3">
        <!-- Stat card 3 -->
    </MudItem>
    <MudItem xs="12" sm="6" md="3">
        <!-- Stat card 4 -->
    </MudItem>
</MudGrid>
```

### Buttons

```html
<!-- Primary Action -->
<MudButton Variant="Variant.Filled" 
           Color="Color.Primary" 
           StartIcon="@Icons.Material.Filled.Add"
           OnClick="AddNewTest">
    Add INR Result
</MudButton>

<!-- Secondary Action -->
<MudButton Variant="Variant.Outlined" 
           Color="Color.Secondary"
           StartIcon="@Icons.Material.Filled.Download"
           OnClick="ExportData">
    Export
</MudButton>

<!-- Icon Button -->
<MudIconButton Icon="@Icons.Material.Filled.Edit" 
               Color="Color.Primary" 
               Size="Size.Small"
               OnClick="EditItem" />

<!-- Button Group -->
<MudButtonGroup OverrideStyles="false">
    <MudButton Variant="@(period == "3m" ? Variant.Filled : Variant.Outlined)" 
               Color="Color.Primary" 
               Size="Size.Small" 
               OnClick="@(() => ChangePeriod("3m"))">3M</MudButton>
    <MudButton Variant="@(period == "6m" ? Variant.Filled : Variant.Outlined)" 
               Color="Color.Primary" 
               Size="Size.Small" 
               OnClick="@(() => ChangePeriod("6m"))">6M</MudButton>
</MudButtonGroup>
```

### Icons

```html
<!-- Material Design Icons -->
<MudIcon Icon="@Icons.Material.Filled.ShowChart" />
<MudIcon Icon="@Icons.Material.Filled.Notifications" Color="Color.Warning" />
<MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" Size="Size.Large" />
<MudIcon Icon="@Icons.Material.Outlined.Info" Color="Color.Info" />

<!-- Common Icons for Blood Thinner Tracker -->
@Icons.Material.Filled.Medication      // Medication pill
@Icons.Material.Filled.Bloodtype       // Blood type/INR
@Icons.Material.Filled.CalendarToday   // Scheduling
@Icons.Material.Filled.ShowChart       // Trends/Charts
@Icons.Material.Filled.Warning         // Warnings
@Icons.Material.Filled.CheckCircle     // Success/In Range
@Icons.Material.Filled.Error           // Error/Out of Range
@Icons.Material.Filled.TrendingUp      // Positive trend
@Icons.Material.Filled.TrendingDown    // Negative trend
```

---

## ❌ What NOT to Use

### Avoid JavaScript Interop
```csharp
// ❌ WRONG - JavaScript interop
@inject IJSRuntime JSRuntime
await JSRuntime.InvokeVoidAsync("alert", "Message");
await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure?");

// ✅ CORRECT - MudBlazor components
@inject ISnackbar Snackbar
@inject IDialogService DialogService

Snackbar.Add("Message", Severity.Info);
var result = await DialogService.ShowMessageBox("Confirm", "Are you sure?");
```

### Avoid External Chart Libraries
```html
<!-- ❌ WRONG - JavaScript chart library -->
<div id="chartDiv"></div>
@code {
    await JSRuntime.InvokeVoidAsync("renderChart", "chartDiv", data);
}

<!-- ✅ CORRECT - MudChart -->
<MudChart ChartType="ChartType.Line" 
          ChartSeries="@chartSeries" 
          XAxisLabels="@labels" />
```

### Avoid Bootstrap JavaScript Components
```html
<!-- ❌ WRONG - Bootstrap modals requiring JavaScript -->
<div class="modal" data-bs-toggle="modal">...</div>

<!-- ✅ CORRECT - MudDialog -->
<MudDialog>
    <DialogContent>...</DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" OnClick="Submit">OK</MudButton>
    </DialogActions>
</MudDialog>
```

---

## Migration Checklist

When working on UI tasks (T018d, T018e, T018f), ensure:

- [ ] No `@inject IJSRuntime JSRuntime` in Razor components
- [ ] No `JSRuntime.InvokeAsync` or `InvokeVoidAsync` calls
- [ ] Charts use `MudChart` component
- [ ] Confirmations use `IDialogService.ShowMessageBox`
- [ ] Notifications use `ISnackbar.Add`
- [ ] Data tables use `MudDataGrid`
- [ ] Form inputs use MudBlazor components (MudTextField, etc.)
- [ ] Icons use `MudIcon` with Material Design icons
- [ ] Buttons use `MudButton` or `MudIconButton`
- [ ] Layout uses `MudCard`, `MudPaper`, `MudGrid`

---

## Resources

- **MudBlazor Documentation**: https://mudblazor.com/
- **Component API**: https://mudblazor.com/api
- **Material Icons**: https://fonts.google.com/icons
- **Migration Guide**: `docs/MUDBLAZOR_MIGRATION.md`
- **GitHub**: https://github.com/MudBlazor/MudBlazor

---

## Getting Help

1. Check MudBlazor documentation: https://mudblazor.com/components/
2. Review `docs/MUDBLAZOR_MIGRATION.md` for examples
3. Look at existing implementation in `INRTracking.razor` (T018b1)
4. MudBlazor Discord: https://discord.gg/mudblazor
