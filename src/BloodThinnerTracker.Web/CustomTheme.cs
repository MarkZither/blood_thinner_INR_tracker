using MudBlazor;
using MudBlazor.Utilities;

namespace BloodThinnerTracker.Web;

public static class CustomTheme
{
    public static MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = new MudColor("#1976D2"),        // Medical blue
            Secondary = new MudColor("#4CAF50"),      // Safety green
            Error = new MudColor("#F44336"),          // Alert red
            Warning = new MudColor("#FF9800"),        // Caution amber
            Info = new MudColor("#2196F3"),           // Info blue
            Success = new MudColor("#4CAF50"),        // Success green
            AppbarBackground = new MudColor("#1976D2"),
            DrawerBackground = new MudColor("#FFFFFF"),
            Background = new MudColor("#FAFAFA"),     // Very light grey instead of #F5F5F5
            BackgroundGray = new MudColor("#F5F5F5"),
            Surface = new MudColor("#FFFFFF"),
            TextPrimary = new MudColor("#212121"),
            TextSecondary = new MudColor("#757575"),
            ActionDefault = new MudColor("#757575"),
            ActionDisabled = new MudColor("#BDBDBD"),
            ActionDisabledBackground = new MudColor("#EEEEEE"),
            LinesDefault = new MudColor("#E0E0E0"),
            Divider = new MudColor("#E0E0E0"),
            TableLines = new MudColor("#E0E0E0")
        },
        PaletteDark = new PaletteDark()
        {
            Primary = new MudColor("#90CAF9"),
            Secondary = new MudColor("#81C784"),
            Error = new MudColor("#EF5350"),
            Warning = new MudColor("#FFB74D"),
            Info = new MudColor("#64B5F6"),
            Success = new MudColor("#81C784"),
            AppbarBackground = new MudColor("#1E1E1E"),
            DrawerBackground = new MudColor("#2D2D30"),
            Background = new MudColor("#1E1E1E"),
            Surface = new MudColor("#2D2D30"),
            TextPrimary = new MudColor("#FFFFFF"),
            TextSecondary = new MudColor("#AAAAAA"),
            ActionDefault = new MudColor("#AAAAAA"),
            ActionDisabled = new MudColor("#4A4A4A"),
            ActionDisabledBackground = new MudColor("#2D2D30")
        },
        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "4px",
            DrawerWidthLeft = "240px",
            DrawerWidthRight = "240px",
            AppbarHeight = "64px"
        }
    };
}
