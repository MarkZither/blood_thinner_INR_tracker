using Xunit;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace BloodThinnerTracker.Mobile.UnitTests;

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

        if (projectDir == null)
            throw new InvalidOperationException("Could not find BloodThinnerTracker.Mobile project");

        return Path.Combine(projectDir, "src", "BloodThinnerTracker.Mobile");
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

        // MAUI uses direct ResourceDictionary children in Application.Resources,
        // NOT ResourceDictionary.MergedDictionaries wrapper
        var mergedDictionaries = resourcesElement?.Elements(ns + "ResourceDictionary");
        Assert.NotNull(mergedDictionaries);
        Assert.NotEmpty(mergedDictionaries);  // Should have theme ResourceDictionaries
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

        // Assert - AppStyles MUST merge AppColors so that {StaticResource TextDark} can be resolved
        // when this ResourceDictionary is initialized by the XAML source generator
        var ns = XNamespace.Get("http://schemas.microsoft.com/dotnet/2021/maui");
        var mergedDictionaries = doc.Root?.Elements(ns + "ResourceDictionary.MergedDictionaries").ToList();

        // MUST have merged dictionaries - critical for AppStyles.xaml.sg.cs to work
        Assert.NotEmpty(mergedDictionaries ?? new List<XElement>());

        // And it should reference AppColors.xaml
        var appColorsRef = mergedDictionaries?.SelectMany(md => md.Elements(ns + "ResourceDictionary"))
            .FirstOrDefault(rd => rd.Attribute("Source")?.Value == "AppColors.xaml");
        Assert.NotNull(appColorsRef);
    }

    [Fact]
    public void AllThemeFiles_ShouldParseWithoutXmlErrors()
    {
        // Arrange
        var themePaths = new[]
        {
            Path.Combine(_projectPath, "App.xaml"),
            Path.Combine(_projectPath, "Themes", "AppColors.xaml"),
            Path.Combine(_projectPath, "Themes", "AppStyles.xaml")
        };

        // Act & Assert
        foreach (var path in themePaths)
        {
            Assert.True(File.Exists(path), $"XAML file not found: {path}");
            var xamlContent = File.ReadAllText(path);

            // Should parse without throwing
            var doc = XDocument.Parse(xamlContent);
            Assert.NotNull(doc.Root);
        }
    }

    [Fact]
    public void AllStaticResourceReferences_ShouldHaveDefinedKeys()
    {
        // Arrange
        var colorsPath = Path.Combine(_projectPath, "Themes", "AppColors.xaml");
        var stylesPath = Path.Combine(_projectPath, "Themes", "AppStyles.xaml");

        var colorContent = File.ReadAllText(colorsPath);
        var styleContent = File.ReadAllText(stylesPath);

        // Parse both files
        var colorDoc = XDocument.Parse(colorContent);
        var styleDoc = XDocument.Parse(styleContent);

        var ns = XNamespace.Get("http://schemas.microsoft.com/dotnet/2021/maui");
        var xns = XNamespace.Get("http://schemas.microsoft.com/winfx/2009/xaml");

        // Collect all defined keys in colors
        var definedKeys = new HashSet<string>();
        foreach (var element in colorDoc.Descendants())
        {
            var keyAttr = element.Attribute(xns + "Key");
            if (keyAttr?.Value != null)
            {
                definedKeys.Add(keyAttr.Value);
            }
        }

        // Verify we have some color definitions
        Assert.NotEmpty(definedKeys);

        // Extract all StaticResource references using regex
        var staticResourcePattern = @"\{StaticResource\s+(\w+)\}";
        var matches = Regex.Matches(styleContent, staticResourcePattern);

        var missingKeys = new List<string>();
        foreach (Match match in matches)
        {
            var keyName = match.Groups[1].Value;
            if (!definedKeys.Contains(keyName))
            {
                missingKeys.Add(keyName);
            }
        }

        // Assert - all referenced keys must be defined
        Assert.True(missingKeys.Count == 0,
            $"The following StaticResource keys are used in AppStyles.xaml but not defined in AppColors.xaml: {string.Join(", ", missingKeys.Distinct())}");
    }

    [Fact]
    public void AppThemeBindingInStyles_ShouldOnlyReferenceDefinedKeys()
    {
        // Arrange
        var colorsPath = Path.Combine(_projectPath, "Themes", "AppColors.xaml");
        var stylesPath = Path.Combine(_projectPath, "Themes", "AppStyles.xaml");

        var colorContent = File.ReadAllText(colorsPath);
        var styleContent = File.ReadAllText(stylesPath);

        // Parse both files
        var colorDoc = XDocument.Parse(colorContent);

        var xns = XNamespace.Get("http://schemas.microsoft.com/winfx/2009/xaml");

        // Collect all defined keys in colors
        var definedKeys = new HashSet<string>();
        foreach (var element in colorDoc.Descendants())
        {
            var keyAttr = element.Attribute(xns + "Key");
            if (keyAttr?.Value != null)
            {
                definedKeys.Add(keyAttr.Value);
            }
        }

        // Extract all AppThemeBinding StaticResource references
        // Pattern: {AppThemeBinding Light={StaticResource KeyName}, Dark={StaticResource KeyName}}
        var appThemeBindingPattern = @"AppThemeBinding\s+Light=\{StaticResource\s+(\w+)\}\s*,\s*Dark=\{StaticResource\s+(\w+)\}";
        var matches = Regex.Matches(styleContent, appThemeBindingPattern);

        var missingKeys = new HashSet<string>();
        foreach (Match match in matches)
        {
            var lightKey = match.Groups[1].Value;
            var darkKey = match.Groups[2].Value;

            if (!definedKeys.Contains(lightKey))
                missingKeys.Add(lightKey);
            if (!definedKeys.Contains(darkKey))
                missingKeys.Add(darkKey);
        }

        // Assert - all AppThemeBinding references must use defined keys
        Assert.True(missingKeys.Count == 0,
            $"The following keys used in AppThemeBinding expressions are not defined in AppColors.xaml: {string.Join(", ", missingKeys.Distinct())}");
    }
}
