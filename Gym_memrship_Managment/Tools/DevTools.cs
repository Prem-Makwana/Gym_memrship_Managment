using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Gym_memrship_Managment.Tools
{
    /// <summary>
    /// Developer utility tools - C# replacement for the old Python helper scripts.
    /// 
    /// Available tools:
    ///   DevTools.AddUtf8Bom()       - Ensures all .cshtml files have a UTF-8 BOM (replaces add_bom.py)
    ///   DevTools.FixMojibake()      - Fixes garbled â€" characters in .cshtml files (replaces fix_mojibake_*.py)
    ///   DevTools.ReplaceCurrency()  - Converts $ currency signs to ₹ in all .cshtml files (replaces replace_currency.py)
    ///   DevTools.GenerateSchemaMd() - Parses Database_Schema.sql and generates schema.md (replaces generate_md.py)
    ///
    /// To run from the Package Manager Console:
    ///   -- These are static methods you can call from any startup/seed script.
    /// </summary>
    public static class DevTools
    {
        private static readonly string ViewsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Views");
        private static readonly string ProjectRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..");

        // ─────────────────────────────────────────────────────────────────────
        // 1. ADD UTF-8 BOM  (replaces add_bom.py)
        //    Ensures every .cshtml file starts with the UTF-8 BOM bytes
        //    so that the Razor compiler always reads them correctly.
        // ─────────────────────────────────────────────────────────────────────
        public static int AddUtf8Bom(string? viewsPath = null)
        {
            string root = viewsPath ?? ViewsPath;
            if (!Directory.Exists(root))
            {
                Console.WriteLine($"[AddUtf8Bom] Views folder not found: {root}");
                return 0;
            }

            byte[] bom = { 0xEF, 0xBB, 0xBF };
            int count = 0;

            foreach (string file in Directory.EnumerateFiles(root, "*.cshtml", SearchOption.AllDirectories))
            {
                byte[] bytes = File.ReadAllBytes(file);

                // Skip if it already has the BOM
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                    continue;

                byte[] withBom = new byte[bytes.Length + 3];
                bom.CopyTo(withBom, 0);
                bytes.CopyTo(withBom, 3);
                File.WriteAllBytes(file, withBom);
                count++;
            }

            Console.WriteLine($"[AddUtf8Bom] Added UTF-8 BOM to {count} file(s).");
            return count;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. FIX MOJIBAKE  (replaces fix_mojibake_clean.py / fix_mojibake_bytes.py)
        //    Repairs garbled UTF-8 characters like â€" → — that appear
        //    when files are read as ANSI and saved back.
        // ─────────────────────────────────────────────────────────────────────
        public static int FixMojibake(string? viewsPath = null)
        {
            string root = viewsPath ?? ViewsPath;
            if (!Directory.Exists(root))
            {
                Console.WriteLine($"[FixMojibake] Views folder not found: {root}");
                return 0;
            }

            // Build mojibake keys from raw Latin-1 bytes so this source file
            // stays clean 7-bit ASCII and the compiler never chokes on them.
            static string L(params byte[] b) => Encoding.Latin1.GetString(b);

            var mappings = new Dictionary<string, string>
            {
                // â€" (0xE2 0x80 0x94) → em dash   —
                { L(0xC3,0xA2,0xE2,0x80,0x9C,0x22),  "\u2014" },
                // â€" (0xE2 0x80 0x93) → en dash   –
                { L(0xC3,0xA2,0xE2,0x80,0x9C,0x22),  "\u2013" },
                // â€¢ → bullet •
                { L(0xC3,0xA2,0xE2,0x82,0xAC,0xC2,0xA2), "\u2022" },
                // â‚¹ → rupee ₹
                { L(0xC3,0xA2,0xE2,0x80,0x9A,0xC2,0xB9), "\u20B9" },
                // â˜… → star ★
                { L(0xC3,0xA2,0xCB,0x9C,0xE2,0x80,0xA6),  "\u2605" },
                // â†' → right arrow →
                { L(0xC3,0xA2,0xE2,0x80,0xA0,0xE2,0x80,0x99), "\u2192" },
                // â€™ → right single quote '
                { L(0xC3,0xA2,0xE2,0x82,0xAC,0xE2,0x84,0xA2), "\u2019" },
                // â€œ → left double quote "
                { L(0xC3,0xA2,0xE2,0x82,0xAC,0xC5,0x93), "\u201C" },
                // â€ → right double quote "
                { L(0xC3,0xA2,0xE2,0x82,0xAC,0xEF,0xBF,0xBD), "\u201D" },
            };

            int count = 0;
            foreach (string file in Directory.EnumerateFiles(root, "*.cshtml", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file, Encoding.UTF8);
                string original = content;

                foreach (var kv in mappings)
                    content = content.Replace(kv.Key, kv.Value);

                if (content != original)
                {
                    File.WriteAllText(file, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                    count++;
                }
            }

            Console.WriteLine($"[FixMojibake] Fixed mojibake in {count} file(s).");
            return count;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. REPLACE CURRENCY  (replaces replace_currency.py)
        //    Converts all $ currency symbols to ₹ (Indian Rupee) in .cshtml files,
        //    but safely skips JavaScript template literals like ${variable}.
        // ─────────────────────────────────────────────────────────────────────
        public static int ReplaceCurrency(string? viewsPath = null)
        {
            string root = viewsPath ?? ViewsPath;
            if (!Directory.Exists(root))
            {
                Console.WriteLine($"[ReplaceCurrency] Views folder not found: {root}");
                return 0;
            }

            int count = 0;
            foreach (string file in Directory.EnumerateFiles(root, "*.cshtml", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(file, Encoding.UTF8);
                string original = content;

                // Replace data-prefix="$" → data-prefix="₹"
                content = content.Replace("data-prefix=\"$\"", "data-prefix=\"₹\"");

                // Replace Discount ($) → Discount (₹)
                content = content.Replace("Discount ($)", "Discount (₹)");

                // Replace '$' + → '₹' +  (JS string concat)
                content = content.Replace("'$' +", "'₹' +");
                content = content.Replace("'-$' +", "'-₹' +");

                // Replace $@ followed by a letter (Razor syntax like $@Model.Amount) → ₹@...
                // This is safe because Razor $@ is NOT JavaScript template literal syntax
                content = Regex.Replace(content, @"\$@([a-zA-Z])", "₹@$1");

                if (content != original)
                {
                    File.WriteAllText(file, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                    count++;
                }
            }

            Console.WriteLine($"[ReplaceCurrency] Updated currency symbol in {count} file(s).");
            return count;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. GENERATE SCHEMA MARKDOWN  (replaces generate_md.py)
        //    Reads Database_Schema.sql and produces a schema.md documentation file
        //    with tables, fields, types, constraints, and descriptions.
        // ─────────────────────────────────────────────────────────────────────
        public static void GenerateSchemaMd(string? sqlFilePath = null, string? outputPath = null)
        {
            string root = Path.GetFullPath(ProjectRoot);
            string sqlPath = sqlFilePath ?? Path.Combine(root, "Database_Schema.sql");
            string mdPath  = outputPath  ?? Path.Combine(root, "schema.md");

            if (!File.Exists(sqlPath))
            {
                Console.WriteLine($"[GenerateSchemaMd] SQL file not found: {sqlPath}");
                Console.WriteLine("  Run: dotnet ef dbcontext script -o Database_Schema.sql");
                return;
            }

            string sql = File.ReadAllText(sqlPath, Encoding.UTF8);

            // Strip GO statements and single-line comments
            sql = Regex.Replace(sql, @"^GO\s*$", "", RegexOptions.Multiline);
            sql = Regex.Replace(sql, @"--[^\r\n]*", "");

            // Find all CREATE TABLE blocks
            var tableMatches = Regex.Matches(
                sql,
                @"CREATE TABLE \[([^\]]+)\] \((.*?)\);",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            );

            var sb = new StringBuilder();
            sb.AppendLine("# Database Schema Documentation");
            sb.AppendLine();

            foreach (Match tableMatch in tableMatches)
            {
                string tableName  = tableMatch.Groups[1].Value;
                string columnsStr = tableMatch.Groups[2].Value;

                if (tableName == "__EFMigrationsHistory") continue;

                var lines = columnsStr.Split('\n');
                var fields = new List<FieldInfo>();
                string pk   = "-";
                var fks     = new List<string>();

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim().TrimEnd(',');
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("CONSTRAINT", StringComparison.OrdinalIgnoreCase))
                    {
                        if (line.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
                        {
                            var m = Regex.Match(line, @"PRIMARY KEY \(\[([^\]]+)\]\)");
                            if (m.Success) pk = m.Groups[1].Value;
                        }
                        else if (line.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                        {
                            var m = Regex.Match(line, @"FOREIGN KEY \(\[([^\]]+)\]\)");
                            if (m.Success) fks.Add(m.Groups[1].Value);
                        }
                    }
                    else
                    {
                        var m = Regex.Match(line, @"^\[([^\]]+)\]\s+([a-zA-Z0-9_\(\)max]+)(.*?)$", RegexOptions.IgnoreCase);
                        if (!m.Success) continue;

                        string colName  = m.Groups[1].Value;
                        string dataType = m.Groups[2].Value.ToLower();
                        string rest     = m.Groups[3].Value.ToUpper();

                        string size = "-";
                        if (dataType.Contains('('))
                        {
                            var parts = dataType.Split('(');
                            dataType = parts[0];
                            size = parts[1].TrimEnd(')');
                            if (dataType == "nvarchar" && size == "max") size = "MAX";
                        }

                        var constraints = new List<string>();
                        constraints.Add(rest.Contains("NOT NULL") ? "NOT NULL" : "NULL");
                        if (rest.Contains("IDENTITY")) constraints.Add("IDENTITY");

                        string desc;
                        if (colName == pk)        { constraints.Add("PK"); desc = $"Primary identifier for {tableName}"; }
                        else if (fks.Contains(colName)) { constraints.Add("FK"); desc = "Foreign key referencing another table"; }
                        else                            { desc = $"Stores {colName.ToLower()} data"; }

                        fields.Add(new FieldInfo(colName, dataType, size, string.Join(", ", constraints), desc));
                    }
                }

                sb.AppendLine($"### {tableName}");
                sb.AppendLine();
                sb.AppendLine($"**Primary Key** : {pk}<br>");
                sb.AppendLine($"**Foreign Key** : {(fks.Count > 0 ? string.Join(", ", fks) : "-")}<br>");
                sb.AppendLine($"**Description** : Stores information and records for {tableName} in the system.");
                sb.AppendLine();
                sb.AppendLine("| Field Name | Data Type | Size | Constraints | Description |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (var f in fields)
                    sb.AppendLine($"| {f.Name} | {f.Type} | {f.Size} | {f.Constraints} | {f.Desc} |");
                sb.AppendLine();
                sb.AppendLine("<br>");
                sb.AppendLine();
            }

            File.WriteAllText(mdPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine($"[GenerateSchemaMd] Schema markdown written to: {mdPath}");
        }

        // ── Internal helper record ─────────────────────────────────────────
        private record FieldInfo(string Name, string Type, string Size, string Constraints, string Desc);
    }
}
