using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Mobile.UnitTests.Resources
{
    public class ResourceKeysExistTests
    {
        // Adjust paths relative to repository root
        private static readonly string MobileProjectPath = Path.Combine("src", "BloodThinnerTracker.Mobile");
        private static readonly string ThemesPath = Path.Combine(MobileProjectPath, "Themes");

        [Fact]
        public void AllStaticResourceKeysReferencedInXaml_AreDefinedInResourceDictionaries()
        {
            var repoRoot = Directory.GetCurrentDirectory();

            // Collect XAML files in mobile project
            var xamlFiles = Directory.GetFiles(MobileProjectPath, "*.xaml", SearchOption.AllDirectories);
            var referencedKeys = new HashSet<string>(StringComparer.Ordinal);

            // Regex patterns to find StaticResource and AppThemeBinding Light/Dark StaticResource usages
            var staticResourcePattern = new Regex("\\{StaticResource\\s+([A-Za-z0-9_.:-]+)\\}", RegexOptions.Compiled);
            var appThemeBindingPattern = new Regex("AppThemeBinding\\s+Light=\\{StaticResource\\s+([A-Za-z0-9_.:-]+)\\}", RegexOptions.Compiled);

            foreach (var file in xamlFiles)
            {
                var text = File.ReadAllText(file);
                foreach (Match m in staticResourcePattern.Matches(text))
                {
                    referencedKeys.Add(m.Groups[1].Value);
                }

                foreach (Match m in appThemeBindingPattern.Matches(text))
                {
                    referencedKeys.Add(m.Groups[1].Value);
                }
            }

            // Load keys defined in theme resource files and App.xaml
            var definedKeys = new HashSet<string>(StringComparer.Ordinal);
            var resourceFiles = new List<string>();
            resourceFiles.Add(Path.Combine(MobileProjectPath, "App.xaml"));
            if (Directory.Exists(ThemesPath)) resourceFiles.AddRange(Directory.GetFiles(ThemesPath, "*.xaml", SearchOption.TopDirectoryOnly));

            var keyPattern = new Regex("x:Key=\"([A-Za-z0-9_.:-]+)\"", RegexOptions.Compiled);

            foreach (var rf in resourceFiles.Where(File.Exists))
            {
                var txt = File.ReadAllText(rf);
                foreach (Match m in keyPattern.Matches(txt))
                {
                    definedKeys.Add(m.Groups[1].Value);
                }
            }

            // Exclusions: keys provided by MAUI or third-party libraries that we don't define
            var allowedMissing = new HashSet<string>(StringComparer.Ordinal)
            {
                // Common MAUI keys or system-like tokens you expect can be added here
                "MauiBorderStyle",
            };

            var missing = referencedKeys.Where(k => !definedKeys.Contains(k) && !allowedMissing.Contains(k)).ToList();

            Assert.True(missing.Count == 0, "Missing resource keys referenced from XAML: " + string.Join(", ", missing));
        }
    }
}
