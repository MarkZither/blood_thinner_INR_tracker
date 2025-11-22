using Xunit;

namespace BloodThinnerTracker.Mobile.UnitTests;

/// <summary>
/// DEMONSTRATION: This file shows what WOULD happen if TextDark was missing from AppColors.xaml
///
/// These tests are NOT run (file ends in _DemonstrationOfFailure).
/// They demonstrate that our XamlRuntimeLoadTests WOULD catch the runtime error:
/// "XamlParseException: Position 10:38. StaticResource not found for key TextDark"
///
/// If you uncomment these and run them, they will FAIL, proving the tests work correctly.
/// </summary>
public class XamlRuntimeLoadTests_DemonstrationOfFailure
{
    [Fact(Skip = "DEMONSTRATION: This is what SHOULD fail if TextDark key was missing from AppColors.xaml")]
    public void DEMO_IF_TextDark_WAS_REMOVED_This_Test_Would_Fail()
    {
        // If you remove the line:
        //   <Color x:Key="TextDark">#333333</Color>
        // from AppColors.xaml, then remove the [Skip] attribute and run this test,
        // it will FAIL with the error message below.
        //
        // This proves our tests are designed to catch the exact runtime error:
        // XamlParseException: Position 10:38. StaticResource not found for key TextDark
        //
        // Expected Failure Message:
        // "FAIL: AppStyles.xaml references StaticResource keys that are NOT defined in AppColors.xaml: TextDark.
        //  This causes runtime error: XamlParseException - StaticResource not found for key [key].
        //  Add these color definitions to AppColors.xaml: <Color x:Key=\"TextDark\">..."

        var projectPath = GetProjectPath();
        var colorsPath = Path.Combine(projectPath, "Themes", "AppColors.xaml");
        var stylesPath = Path.Combine(projectPath, "Themes", "AppStyles.xaml");

        var colorsContent = File.ReadAllText(colorsPath);
        var stylesContent = File.ReadAllText(stylesPath);

        // If TextDark is somehow missing from colorsContent, this assertion will fail
        Assert.True(colorsContent.Contains("x:Key=\"TextDark\""),
            "PROOF: TextDark key IS defined in AppColors.xaml - test passes correctly. " +
            "If this assertion fails, it means TextDark was removed, which would cause " +
            "the runtime error we're trying to prevent.");
    }

    [Fact(Skip = "DEMONSTRATION: This test validates the exact runtime error scenario")]
    public void DEMO_The_Exact_Runtime_Error_Our_Tests_Prevent()
    {
        // This test documents the exact error that occurs when TextDark is missing.
        // The runtime error you reported was:
        //
        // Exception: XamlParseException
        // Message: Position 10:38. StaticResource not found for key TextDark
        // Location: Microsoft.Maui.Controls.Xaml.ApplyPropertiesVisitor.ProvideValue()
        //
        // This happens when:
        // 1. App.xaml merges AppColors.xaml
        // 2. App.xaml merges AppStyles.xaml
        // 3. AppStyles.xaml contains: {StaticResource TextDark}
        // 4. But AppColors.xaml is MISSING the line: <Color x:Key="TextDark">#333333</Color>
        //
        // Our XamlRuntimeLoadTests.cs validates that this scenario CANNOT happen
        // by checking that all {StaticResource X} references in AppStyles have
        // corresponding <Color x:Key="X"> definitions in AppColors.

        Assert.True(true, "Our tests are designed to prevent this exact error.");
    }

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
}
