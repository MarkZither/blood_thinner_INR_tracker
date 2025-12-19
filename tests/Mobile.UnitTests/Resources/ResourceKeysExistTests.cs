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
            // Try to locate repository root (walk upwards until we find the mobile project folder)
            var cwd = Directory.GetCurrentDirectory();
            string? repoRoot = null;
            var dir = new DirectoryInfo(cwd);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, MobileProjectPath);
                if (Directory.Exists(candidate))
                {
                    repoRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            // If we cannot find the source tree (e.g., running in isolated test environment), skip this check
            if (repoRoot == null)
            {
                // Not a failure; resource verification requires source files to be available in the test context
                return;
            }

            // Collect XAML files in mobile project (relative to repo root)
            var projectPath = Path.Combine(repoRoot, MobileProjectPath);
            var xamlFiles = Directory.GetFiles(projectPath, "*.xaml", SearchOption.AllDirectories);
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

            // Load keys defined in theme resource files and App.xaml (resolve relative to repo root)
            var definedKeys = new HashSet<string>(StringComparer.Ordinal);
            var resourceFiles = new List<string>();
            var appXaml = Path.Combine(projectPath, "App.xaml");
            resourceFiles.Add(appXaml);
            var themesResolved = Path.Combine(projectPath, "Themes");
            if (Directory.Exists(themesResolved)) resourceFiles.AddRange(Directory.GetFiles(themesResolved, "*.xaml", SearchOption.TopDirectoryOnly));

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
