using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Mobile.UnitTests.Android
{
    public class ResourceDrawableReferenceTests
    {
        private static readonly string AndroidResPath = Path.Combine("src", "BloodThinnerTracker.Mobile", "Platforms", "Android", "Resources");

        [Fact]
        public void DrawableXml_Should_Not_Reference_Missing_Resources()
        {
            if (!Directory.Exists(AndroidResPath))
            {
                // Nothing to validate on non-Android environments; pass the test.
                return;
            }

            var drawableDir = Path.Combine(AndroidResPath, "drawable");
            if (!Directory.Exists(drawableDir)) return;

            // Gather available resource names from values/*.xml and drawables
            var definedStyles = new HashSet<string>(StringComparer.Ordinal);
            var definedColors = new HashSet<string>(StringComparer.Ordinal);
            var definedDrawables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // drawables by file name (without extension)
            foreach (var f in Directory.GetFiles(drawableDir))
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (!string.IsNullOrWhiteSpace(name)) definedDrawables.Add(name);
            }

            var valuesDir = Path.Combine(AndroidResPath, "values");
            if (Directory.Exists(valuesDir))
            {
                foreach (var vf in Directory.GetFiles(valuesDir, "*.xml"))
                {
                    try
                    {
                        var doc = XDocument.Load(vf);
                        foreach (var style in doc.Descendants("style"))
                        {
                            var name = style.Attribute("name")?.Value;
                            if (!string.IsNullOrEmpty(name)) definedStyles.Add(name);
                        }

                        foreach (var color in doc.Descendants("color"))
                        {
                            var name = color.Attribute("name")?.Value;
                            if (!string.IsNullOrEmpty(name)) definedColors.Add(name);
                        }

                        // also look for <item name="..."> entries inside resources for types
                        foreach (var item in doc.Descendants("item"))
                        {
                            var name = item.Attribute("name")?.Value;
                            var typeAttr = item.Attribute("type")?.Value;
                            if (!string.IsNullOrEmpty(name) && string.Equals(typeAttr, "color", StringComparison.OrdinalIgnoreCase))
                            {
                                definedColors.Add(name);
                            }
                        }
                    }
                    catch
                    {
                        // ignore malformed value files for the purpose of this test
                    }
                }
            }

            var missing = new List<string>();

            // Patterns to find resource references like @style/Name or @color/name or @drawable/name
            var resRefPattern = new Regex("@(?<type>style|color|drawable)/(?<name>[A-Za-z0-9_\\.:-]+)", RegexOptions.Compiled);

            foreach (var xmlFile in Directory.GetFiles(drawableDir, "*.xml"))
            {
                XDocument doc = null;
                try
                {
                    doc = XDocument.Load(xmlFile);
                }
                catch
                {
                    // If drawable XML is malformed, flag it as missing
                    missing.Add($"Malformed XML: {xmlFile}");
                    continue;
                }

                var xmlText = File.ReadAllText(xmlFile);
                foreach (Match m in resRefPattern.Matches(xmlText))
                {
                    var type = m.Groups["type"].Value;
                    var name = m.Groups["name"].Value;

                    // Ignore framework attributes like @android:style/ or ?android:attr/
                    if (name.StartsWith("android:", StringComparison.OrdinalIgnoreCase)) continue;

                    switch (type)
                    {
                        case "style":
                            if (!definedStyles.Contains(name)) missing.Add($"{Path.GetFileName(xmlFile)} -> @style/{name}");
                            break;
                        case "color":
                            if (!definedColors.Contains(name)) missing.Add($"{Path.GetFileName(xmlFile)} -> @color/{name}");
                            break;
                        case "drawable":
                            if (!definedDrawables.Contains(name)) missing.Add($"{Path.GetFileName(xmlFile)} -> @drawable/{name}");
                            break;
                    }
                }
            }

            Assert.True(missing.Count == 0, "Missing Android resources referenced from drawable XML: " + string.Join(", ", missing));
        }
    }
}
