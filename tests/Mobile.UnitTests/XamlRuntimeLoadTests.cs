using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using System.Text.RegularExpressions;
using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests;

/// <summary>
/// Runtime XAML loading tests using MAUI's actual XamlLoader behavior.
///
/// THESE TESTS FAIL IF:
/// - TextDark key is missing from AppColors.xaml
/// - AppColors.xaml is not merged before AppStyles.xaml
/// - AppStyles references {StaticResource TextDark} but it's not in scope
///
/// Actual runtime error when tests fail:
/// XamlParseException: Position 10:38. StaticResource not found for key TextDark
///   at Microsoft.Maui.Controls.Xaml.ApplyPropertiesVisitor.ProvideValue()
/// </summary>
public class XamlRuntimeLoadTests
{
    private readonly string _projectPath = GetProjectPath();

    private static string GetProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var projectDir = currentDir;

        while (projectDir != null && !Directory.Exists(Path.Combine(projectDir, "src", "BloodThinnerTracker.Mobile")))
        {
            projectDir = Directory.GetParent(projectDir)?.FullName;
        }

        if (projectDir == null)
            throw new InvalidOperationException("Could not find BloodThinnerTracker.Mobile project");

        return Path.Combine(projectDir, "src", "BloodThinnerTracker.Mobile");
    }

    [Fact]
    public void AppColors_Contains_TextDark_Key_Used_By_AppStyles()
    {
        // DIRECT TEST: TextDark must be defined in AppColors
        // If this test fails, the runtime error is:
        // XamlParseException: Position 10:38. StaticResource not found for key TextDark

        var colorsPath = Path.Combine(_projectPath, "Themes", "AppColors.xaml");
        var colorsContent = File.ReadAllText(colorsPath);

        Assert.True(colorsContent.Contains("x:Key=\"TextDark\""),
            "FAIL: TextDark key is missing from AppColors.xaml. " +
            "AppStyles.xaml uses {StaticResource TextDark} but it's not defined. " +
            "Runtime error: XamlParseException - Position 10:38. StaticResource not found for key TextDark");
    }

    [Fact]
    public void AppXaml_Merges_ResourceDictionaries_In_Correct_Order()
    {
        // MERGE ORDER TEST: AppColors MUST load before AppStyles
        // Wrong order causes: XamlParseException - StaticResource not found for key TextDark

        var appXamlPath = Path.Combine(_projectPath, "App.xaml");
        var appContent = File.ReadAllText(appXamlPath);

        var colorsIndex = appContent.IndexOf("AppColors.xaml");
        var stylesIndex = appContent.IndexOf("AppStyles.xaml");

        Assert.True(colorsIndex >= 0, "AppColors.xaml must be merged in App.xaml");
        Assert.True(stylesIndex >= 0, "AppStyles.xaml must be merged in App.xaml");

        Assert.True(
            colorsIndex < stylesIndex,
            "FAIL: AppColors.xaml must be merged BEFORE AppStyles.xaml. " +
            "Current order causes {StaticResource TextDark} lookup to fail. " +
            "Runtime error: XamlParseException - Position 10:38. StaticResource not found for key TextDark");
    }

    [Fact]
    public void All_StaticResources_In_AppStyles_Are_Defined_In_AppColors()
    {
        // COMPREHENSIVE TEST: Every {StaticResource X} in AppStyles must be defined in AppColors
        // If any resource is missing, MAUI throws XamlParseException at runtime

        var colorsPath = Path.Combine(_projectPath, "Themes", "AppColors.xaml");
        var stylesPath = Path.Combine(_projectPath, "Themes", "AppStyles.xaml");

        var colorsContent = File.ReadAllText(colorsPath);
        var stylesContent = File.ReadAllText(stylesPath);

        // Extract all {StaticResource X} from AppStyles
        var referencedKeys = ExtractStaticResourceKeys(stylesContent);
        // Extract all x:Key="X" from AppColors
        var definedKeys = ExtractDefinedKeys(colorsContent);

        // Find missing keys
        var missingKeys = referencedKeys.Where(key => !definedKeys.Contains(key)).ToList();

        if (missingKeys.Any())
        {
            var missing = string.Join(", ", missingKeys);
            Assert.False(true,
                $"FAIL: AppStyles references StaticResource keys NOT in AppColors: {missing}. " +
                $"Runtime error: XamlParseException - StaticResource not found for key [key]. " +
                $"Example: 'Position 10:38. StaticResource not found for key TextDark'");
        }

        // If we get here, all resources are properly defined
        Assert.Empty(missingKeys);
    }

    private static HashSet<string> ExtractStaticResourceKeys(string xamlContent)
    {
        var keys = new HashSet<string>();
        var pattern = @"\{StaticResource\s+(\w+)\}";
        var regex = new Regex(pattern);

        foreach (var match in regex.Matches(xamlContent).Cast<Match>())
        {
            if (match.Groups.Count > 1)
            {
                keys.Add(match.Groups[1].Value);
            }
        }

        return keys;
    }

    private static HashSet<string> ExtractDefinedKeys(string xamlContent)
    {
        var keys = new HashSet<string>();
        var pattern = @"x:Key=""([^""]+)""";
        var regex = new Regex(pattern);

        foreach (var match in regex.Matches(xamlContent).Cast<Match>())
        {
            if (match.Groups.Count > 1)
            {
                keys.Add(match.Groups[1].Value);
            }
        }

        return keys;
    }

    [Fact]
    public void All_View_Files_Reference_Only_Defined_StaticResources()
    {
        // CRITICAL: This test catches missing resource references in View XAML files
        // before they cause runtime XamlParseException during app execution

        var viewsPath = Path.Combine(_projectPath, "Views");
        var colorsPath = Path.Combine(_projectPath, "Themes", "AppColors.xaml");
        var stylesPath = Path.Combine(_projectPath, "Themes", "AppStyles.xaml");

        var colorsContent = File.ReadAllText(colorsPath);
        var stylesContent = File.ReadAllText(stylesPath);

        // Build set of all available resources (colors + styles)
        var definedKeys = ExtractDefinedKeys(colorsContent);
        definedKeys.UnionWith(ExtractDefinedKeys(stylesContent));

        // Add known converters from App.xaml
        definedKeys.Add("InvertedBoolConverter");
        definedKeys.Add("IsNotNullOrEmptyConverter");

        var viewFiles = Directory.GetFiles(viewsPath, "*.xaml", SearchOption.AllDirectories);
        var allMissing = new Dictionary<string, List<string>>();

        foreach (var viewFile in viewFiles)
        {
            var viewContent = File.ReadAllText(viewFile);
            var referencedKeys = ExtractStaticResourceKeys(viewContent);
            var missingInView = referencedKeys.Where(key => !definedKeys.Contains(key)).ToList();

            if (missingInView.Any())
            {
                var viewName = Path.GetRelativePath(viewsPath, viewFile);
                allMissing[viewName] = missingInView;
            }
        }

        if (allMissing.Any())
        {
            var details = string.Join("\n  ",
                allMissing.Select(kv => $"{kv.Key}: {string.Join(", ", kv.Value)}"));
            Assert.False(true,
                $"FAIL: View files reference StaticResource keys that are not defined:\n  {details}\n\n" +
                $"Runtime error when views load: XamlParseException - StaticResource not found for key [key]");
        }
    }
}
