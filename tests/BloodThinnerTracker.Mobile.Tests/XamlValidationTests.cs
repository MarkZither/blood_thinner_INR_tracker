using Xunit;
using System.Reflection;
using System.Xml.Linq;

namespace BloodThinnerTracker.Mobile.Tests;

/// <summary>
/// Tests to validate XAML structure and resource dictionary syntax.
/// Ensures no startup errors from XAML parsing issues.
/// </summary>
public class XamlValidationTests
{
    private readonly string _projectPath = GetProjectPath();

    private static string GetProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var projectDir = currentDir;

        // Navigate up from test directory to find the Mobile project
        while (projectDir != null && !Directory.Exists(Path.Combine(projectDir, "src", "BloodThinnerTracker.Mobile")))
        {
            projectDir = Directory.GetParent(projectDir)?.FullName;
        }

        return projectDir != null ? Path.Combine(projectDir, "src", "BloodThinnerTracker.Mobile") : null;
    }

    [Fact]
    public void AppXaml_ShouldHaveValidMergedDictionariesSyntax()
    {
        // Arrange
        var appXamlPath = Path.Combine(_projectPath, "App.xaml");
        Assert.True(File.Exists(appXamlPath), $"App.xaml not found at {appXamlPath}");

        // Act
        var xamlContent = File.ReadAllText(appXamlPath);
        var doc = XDocument.Parse(xamlContent);

        // Assert - Check for valid MAUI syntax
        var ns = XNamespace.Get("http://schemas.microsoft.com/dotnet/2021/maui");
        var resourcesElement = doc.Root?.Element(ns + "Application.Resources");
        Assert.NotNull(resourcesElement);

        // MAUI uses Application.Resources directly, not ResourceDictionary wrapper
        var mergedDictionaries = resourcesElement?.Elements(ns + "ResourceDictionary.MergedDictionaries");
        Assert.NotNull(mergedDictionaries);
        Assert.NotEmpty(mergedDictionaries);
    }

    [Fact]
    public void AppColorsXaml_ShouldBeValidResourceDictionary()
    {
        // Arrange
        var colorsPath = Path.Combine(_projectPath, "Themes", "AppColors.xaml");
        Assert.True(File.Exists(colorsPath), $"AppColors.xaml not found at {colorsPath}");

        // Act
        var xamlContent = File.ReadAllText(colorsPath);
        var doc = XDocument.Parse(xamlContent);

        // Assert
        var ns = XNamespace.Get("http://schemas.microsoft.com/dotnet/2021/maui");
        var root = doc.Root;
        Assert.NotNull(root);
        Assert.Equal("ResourceDictionary", root.Name.LocalName);
    }

    [Fact]
    public void AppStylesXaml_ShouldNotHaveMergedDictionariesAtRoot()
    {
        // Arrange
        var stylesPath = Path.Combine(_projectPath, "Themes", "AppStyles.xaml");
        Assert.True(File.Exists(stylesPath), $"AppStyles.xaml not found at {stylesPath}");

        // Act
        var xamlContent = File.ReadAllText(stylesPath);
        var doc = XDocument.Parse(xamlContent);

        // Assert - AppStyles should be a pure ResourceDictionary without MergedDictionaries
        // (it relies on colors already loaded by App.xaml)
        var ns = XNamespace.Get("http://schemas.microsoft.com/dotnet/2021/maui");
        var mergedDictionaries = doc.Root?.Elements(ns + "ResourceDictionary.MergedDictionaries");

        // Should NOT have merged dictionaries - colors come from App.xaml
        Assert.Empty(mergedDictionaries ?? Enumerable.Empty<XElement>());
    }

    [Fact]
    public void AppColorsXaml_ShouldNotUseAppThemeBindingInColorDefinitions()
    {
        // Arrange
        var colorsPath = Path.Combine(_projectPath, "Themes", "AppColors.xaml");
        Assert.True(File.Exists(colorsPath), $"AppColors.xaml not found at {colorsPath}");

        // Act
        var xamlContent = File.ReadAllText(colorsPath);

        // Assert - AppThemeBinding should NOT appear in Color property values
        // (it causes XC0040 error: "Cannot convert value to Color")
        // Valid colors: #0066CC (hex)
        // Invalid: {AppThemeBinding Light=..., Dark=...}
        Assert.DoesNotContain("Color x:Key", xamlContent.Where(c => c.ToString()).Aggregate((a, b) => a + b.ToString())
            .Split('{').Where(s => s.Contains("AppThemeBinding")).FirstOrDefault() ?? "");
    }
}
