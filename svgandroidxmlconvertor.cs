// run me with dotnet run svgandroidxmlconvertor.cs
using System;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;

class SvgVectorConverter
{
    public static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  convert svg2vd input.svg output.xml");
            Console.WriteLine("  convert vd2svg input.xml output.svg");
            return;
        }

        string mode = args[0].ToLower();
        string inputPath = args[1];
        string outputPath = args[2];

        if (!File.Exists(inputPath))
        {
            Console.WriteLine("Error: Input file not found.");
            return;
        }

        try
        {
            if (mode == "svg2vd")
            {
                ConvertSvgToVectorDrawable(inputPath, outputPath);
            }
            else if (mode == "vd2svg")
            {
                ConvertVectorDrawableToSvg(inputPath, outputPath);
            }
            else
            {
                Console.WriteLine("Invalid mode. Use 'svg2vd' or 'vd2svg'.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Conversion failed: " + ex.ToString());
        }
    }

    static void ConvertSvgToVectorDrawable(string svgPath, string xmlPath)
    {
        // Read raw SVG text
        string svgText = File.ReadAllText(svgPath);

        // Quick pre-sanitization: remove common xmlns declarations for inkscape/sodipodi
        svgText = Regex.Replace(svgText, "\\s+xmlns:(?:inkscape|sodipodi)=\"[^\"]*\"", "", RegexOptions.IgnoreCase);

        // Instead of relying on XML parser (which can fail on namespaced element names
        // produced by some editors), extract supported element types with a tolerant regex.
        // This will match optionally-prefixed tags like <path ...> or <svg:path ...>
        var elemTagPattern = new Regex("<\\s*(?:[A-Za-z0-9_\\-]+:)?(path|circle|rect|ellipse|line|polyline|polygon)\\b([^>]*)/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var attrPattern = new Regex("([A-Za-z0-9_\\-:]+)\\s*=\\s*\"([^\"]*)\"", RegexOptions.Compiled);

        // Create VectorDrawable root using XNamespace to avoid constructing names with ':'
        XNamespace androidNs = "http://schemas.android.com/apk/res/android";
        XElement? vector = null;
        try
        {
            vector = new XElement("vector",
                new XAttribute(XNamespace.Xmlns + "android", androidNs.NamespaceName),
                new XAttribute(androidNs + "width", "24dp"),
                new XAttribute(androidNs + "height", "24dp"),
                new XAttribute(androidNs + "viewportWidth", "24"),
                new XAttribute(androidNs + "viewportHeight", "24")
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed creating vector root: " + ex);
            throw;
        }

        int found = 0;

        // Matrix helper for transforms
        static double[] Identity() => new double[] { 1, 0, 0, 1, 0, 0 };
        static double[] Multiply(double[] a, double[] b)
        {
            // a * b (SVG matrices): [a0 a1 a2 a3 a4 a5] -> matrix [a0 a1 a2; a3 a4 a5]? but using SVG order
            // Using convention: matrix(a b c d e f) maps x' = a*x + c*y + e; y' = b*x + d*y + f
            double A = a[0] * b[0] + a[2] * b[1];
            double B = a[1] * b[0] + a[3] * b[1];
            double C = a[0] * b[2] + a[2] * b[3];
            double D = a[1] * b[2] + a[3] * b[3];
            double E = a[0] * b[4] + a[2] * b[5] + a[4];
            double F = a[1] * b[4] + a[3] * b[5] + a[5];
            return new double[] { A, B, C, D, E, F };
        }
        static double[] ParseTransform(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Identity();
            double[] m = Identity();
            // support multiple transforms like "translate(10,20) scale(2)"
            var func = new Regex("([a-zA-Z]+)\\s*\\(([^)]+)\\)", RegexOptions.Compiled);
            foreach (Match fm in func.Matches(s))
            {
                var name = fm.Groups[1].Value.Trim().ToLowerInvariant();
                var args = fm.Groups[2].Value.Split(new char[] {',', ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    if (name == "translate")
                    {
                        double tx = double.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
                        double ty = args.Length > 1 ? double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
                        var t = new double[] { 1, 0, 0, 1, tx, ty };
                        m = Multiply(m, t);
                    }
                    else if (name == "scale")
                    {
                        double sx = double.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
                        double sy = args.Length > 1 ? double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : sx;
                        var t = new double[] { sx, 0, 0, sy, 0, 0 };
                        m = Multiply(m, t);
                    }
                    else if (name == "matrix")
                    {
                        if (args.Length >= 6)
                        {
                            double a = double.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
                            double b = double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
                            double c = double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
                            double d = double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
                            double e = double.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture);
                            double f = double.Parse(args[5], System.Globalization.CultureInfo.InvariantCulture);
                            var t = new double[] { a, b, c, d, e, f };
                            m = Multiply(m, t);
                        }
                    }
                    else if (name == "rotate")
                    {
                        // rotate(angle [cx cy])
                        double angle = double.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
                        double cx = 0, cy = 0;
                        if (args.Length == 3)
                        {
                            cx = double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
                            cy = double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        double rad = angle * Math.PI / 180.0;
                        double cos = Math.Cos(rad);
                        double sin = Math.Sin(rad);
                        // translate(cx,cy) * rotate * translate(-cx,-cy)
                        var toOrigin = new double[] { 1, 0, 0, 1, -cx, -cy };
                        var rot = new double[] { cos, sin * -1 * -1, sin, cos, 0, 0 };
                        // Note: correct mapping: matrix(a b c d e f) -> x' = a*x + c*y + e; y' = b*x + d*y + f
                        // For rotation: a=cos, b=sin, c=-sin, d=cos
                        rot = new double[] { cos, Math.Sin(rad), -Math.Sin(rad), cos, 0, 0 };
                        var back = new double[] { 1, 0, 0, 1, cx, cy };
                        m = Multiply(m, back);
                        m = Multiply(m, rot);
                        m = Multiply(m, toOrigin);
                    }
                }
                catch
                {
                    // ignore malformed transform args and continue
                }
            }
            return m;
        }

        // helper to apply matrix to a point
        static (double x, double y) Apply(double[] mtx, double x, double y)
        {
            double nx = mtx[0] * x + mtx[2] * y + mtx[4];
            double ny = mtx[1] * x + mtx[3] * y + mtx[5];
            return (nx, ny);
        }

        foreach (Match m in elemTagPattern.Matches(svgText))
        {
            found++;
            string tag = m.Groups[1].Value.ToLowerInvariant();
            string attrText = m.Groups[2].Value;
            var attrs = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match a in attrPattern.Matches(attrText))
            {
                string name = a.Groups[1].Value;
                string val = a.Groups[2].Value;
                int colon = name.IndexOf(':');
                if (colon >= 0) name = name.Substring(colon + 1);
                attrs[name] = val;
            }

            string? fill = attrs.ContainsKey("fill") ? attrs["fill"] : null;
            if (string.IsNullOrWhiteSpace(fill))
            {
                fill = "#000000"; // default to black when unspecified
            }
            else if (string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase) || string.Equals(fill, "transparent", StringComparison.OrdinalIgnoreCase))
            {
                // Android VectorDrawable does not accept 'none' — use an explicit transparent color
                fill = "#00000000"; // ARGB transparent
            }
            string transform = attrs.ContainsKey("transform") ? attrs["transform"] : string.Empty;
            var mtx = ParseTransform(transform);

            string d = string.Empty;
            if (tag == "path")
            {
                d = attrs.ContainsKey("d") ? attrs["d"] : string.Empty;
            }
            else if (tag == "circle")
            {
                double cx = attrs.ContainsKey("cx") ? double.Parse(attrs["cx"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double cy = attrs.ContainsKey("cy") ? double.Parse(attrs["cy"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double r = attrs.ContainsKey("r") ? double.Parse(attrs["r"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                // apply transform to key points
                var (x1, y1) = Apply(mtx, cx + r, cy);
                var (x2, y2) = Apply(mtx, cx - r, cy);
                var (x3, y3) = Apply(mtx, cx + r, cy); // back to start
                d = string.Format(System.Globalization.CultureInfo.InvariantCulture, "M {0} {1} A {2} {2} 0 1 0 {3} {4} A {2} {2} 0 1 0 {5} {6} Z", x1, y1, r, x2, y2, x3, y3);
            }
            else if (tag == "ellipse")
            {
                double cx = attrs.ContainsKey("cx") ? double.Parse(attrs["cx"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double cy = attrs.ContainsKey("cy") ? double.Parse(attrs["cy"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double rx = attrs.ContainsKey("rx") ? double.Parse(attrs["rx"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double ry = attrs.ContainsKey("ry") ? double.Parse(attrs["ry"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                var (x1, y1) = Apply(mtx, cx + rx, cy);
                var (x2, y2) = Apply(mtx, cx - rx, cy);
                var (x3, y3) = Apply(mtx, cx + rx, cy);
                d = string.Format(System.Globalization.CultureInfo.InvariantCulture, "M {0} {1} A {2} {3} 0 1 0 {4} {5} A {2} {3} 0 1 0 {6} {7} Z", x1, y1, rx, ry, x2, y2, x3, y3);
            }
            else if (tag == "rect")
            {
                double x = attrs.ContainsKey("x") ? double.Parse(attrs["x"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double y = attrs.ContainsKey("y") ? double.Parse(attrs["y"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double w = attrs.ContainsKey("width") ? double.Parse(attrs["width"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double h = attrs.ContainsKey("height") ? double.Parse(attrs["height"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                var p1 = Apply(mtx, x, y);
                var p2 = Apply(mtx, x + w, y);
                var p3 = Apply(mtx, x + w, y + h);
                var p4 = Apply(mtx, x, y + h);
                d = string.Format(System.Globalization.CultureInfo.InvariantCulture, "M {0} {1} L {2} {3} L {4} {5} L {6} {7} Z", p1.x, p1.y, p2.x, p2.y, p3.x, p3.y, p4.x, p4.y);
            }
            else if (tag == "line")
            {
                double x1v = attrs.ContainsKey("x1") ? double.Parse(attrs["x1"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double y1v = attrs.ContainsKey("y1") ? double.Parse(attrs["y1"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double x2v = attrs.ContainsKey("x2") ? double.Parse(attrs["x2"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                double y2v = attrs.ContainsKey("y2") ? double.Parse(attrs["y2"], System.Globalization.CultureInfo.InvariantCulture) : 0;
                var p1 = Apply(mtx, x1v, y1v);
                var p2 = Apply(mtx, x2v, y2v);
                d = string.Format(System.Globalization.CultureInfo.InvariantCulture, "M {0} {1} L {2} {3}", p1.x, p1.y, p2.x, p2.y);
            }
            else if (tag == "polyline" || tag == "polygon")
            {
                if (attrs.ContainsKey("points"))
                {
                    var ptsText = attrs["points"].Trim();
                        var parts = Regex.Split(ptsText, @"[\s,]+");
                    var coords = new System.Collections.Generic.List<(double x, double y)>();
                    for (int i = 0; i + 1 < parts.Length; i += 2)
                    {
                        if (double.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double px) &&
                            double.TryParse(parts[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double py))
                        {
                            coords.Add(Apply(mtx, px, py));
                        }
                    }
                    if (coords.Count > 0)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "M {0} {1}", coords[0].x, coords[0].y);
                        for (int i = 1; i < coords.Count; i++) sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, " L {0} {1}", coords[i].x, coords[i].y);
                        if (tag == "polygon") sb.Append(" Z");
                        d = sb.ToString();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(d))
            {
                try
                {
                    vector.Add(new XElement("path",
                        new XAttribute(androidNs + "pathData", d),
                        new XAttribute(androidNs + "fillColor", fill)
                    ));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed adding generated path for <{tag}> (d length={d?.Length}): {ex}");
                    throw;
                }
            }
        }

        if (found == 0)
        {
            Console.WriteLine("Warning: no <path> elements found in SVG. Output will be empty.");
        }

        XDocument vdDoc = new XDocument(vector!);
        vdDoc.Save(xmlPath);
        Console.WriteLine($"SVG → VectorDrawable saved to {xmlPath} (paths: {found})");
    }

    static void ConvertVectorDrawableToSvg(string xmlPath, string svgPath)
    {
        XDocument vdDoc = XDocument.Load(xmlPath);
        XNamespace androidNs = "http://schemas.android.com/apk/res/android";
        XNamespace svgNs = "http://www.w3.org/2000/svg";

        XElement svg = new XElement(svgNs + "svg",
            new XAttribute("width", "24"),
            new XAttribute("height", "24"),
            new XAttribute("viewBox", "0 0 24 24")
        );

        foreach (var path in vdDoc.Descendants("path"))
        {
            string d = path.Attribute(androidNs + "pathData")?.Value ?? string.Empty;
            string fill = path.Attribute(androidNs + "fillColor")?.Value ?? "#000000";

            svg.Add(new XElement(svgNs + "path",
                new XAttribute("d", d),
                new XAttribute("fill", fill)
            ));
        }

        XDocument svgDoc = new XDocument(svg);
        svgDoc.Save(svgPath);
        Console.WriteLine($"VectorDrawable → SVG saved to {svgPath}");
    }
}
