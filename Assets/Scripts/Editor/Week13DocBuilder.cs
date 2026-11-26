using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 13 submission document builder.
///
/// Menu: Tools > Dama > Week 13 > 3. Build Submission HTML
///
/// Converts the three markdown documents in docs/Charibelle Guzman/Week_13/others/
/// into print-ready HTML beside them. The HTML is generated from the .md rather than
/// written by hand, so the two can never disagree, and the print stylesheet lives in
/// one place. The PDFs at the week's root are then printed from that HTML.
///
/// Supports the small markdown subset the documents actually use: ATX headings,
/// paragraphs, tables, bullet and numbered lists, fenced code, inline code, bold,
/// italic, horizontal rules and a figure image.
/// </summary>
public static class Week13DocBuilder
{
    private const string Menu = "Tools/Dama/Week 13/3. Build Submission HTML";

    private static readonly string[] Documents =
    {
        "Guzman_Charibelle_SA5",
        "Guzman_Charibelle_PT5",
        "Guzman_Charibelle_Week13_Reflection"
    };

    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

    public static string OthersDir =>
        Path.Combine(ProjectRoot, "docs", "Charibelle Guzman", "Week_13", "others");

    [MenuItem(Menu)]
    public static void BuildAll()
    {
        if (!Directory.Exists(OthersDir))
        {
            Debug.LogError($"[Week13Docs] Folder not found: {OthersDir}");
            return;
        }

        int written = 0;

        foreach (string name in Documents)
        {
            string source = Path.Combine(OthersDir, name + ".md");
            if (!File.Exists(source))
            {
                Debug.LogWarning($"[Week13Docs] Missing source, skipped: {source}");
                continue;
            }

            string html = Convert(File.ReadAllText(source, Encoding.UTF8));
            string target = Path.Combine(OthersDir, name + ".html");
            File.WriteAllText(target, html, new UTF8Encoding(false));
            Debug.Log($"[Week13Docs] Wrote {target}");
            written++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[Week13Docs] {written} document(s) built in {OthersDir}");
    }

    // =====================================================================
    // Markdown -> HTML
    // =====================================================================

    private static string Convert(string markdown)
    {
        string[] lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        StringBuilder body = new StringBuilder();
        List<string> paragraph = new List<string>();
        string title = "Dama - Week 13";
        bool inHeaderBlock = true;   // the **Key:** value block before the first rule
        int index = 0;

        void FlushParagraph()
        {
            if (paragraph.Count == 0) return;
            body.AppendLine("<p>" + Inline(string.Join(" ", paragraph)) + "</p>");
            paragraph.Clear();
        }

        while (index < lines.Length)
        {
            string line = lines[index];
            string trimmed = line.Trim();

            // Fenced code block
            if (trimmed.StartsWith("```"))
            {
                FlushParagraph();
                index++;
                StringBuilder code = new StringBuilder();
                while (index < lines.Length && !lines[index].Trim().StartsWith("```"))
                {
                    code.AppendLine(Escape(lines[index]));
                    index++;
                }
                index++; // closing fence
                body.AppendLine("<pre><code>" + code.ToString().TrimEnd('\n') + "</code></pre>");
                continue;
            }

            // Table
            if (trimmed.StartsWith("|"))
            {
                FlushParagraph();
                List<string> rows = new List<string>();
                while (index < lines.Length && lines[index].Trim().StartsWith("|"))
                {
                    rows.Add(lines[index].Trim());
                    index++;
                }
                AppendTable(body, rows);
                continue;
            }

            // Horizontal rule
            if (trimmed == "---" || trimmed == "***")
            {
                FlushParagraph();
                inHeaderBlock = false;
                body.AppendLine("<hr/>");
                index++;
                continue;
            }

            // Headings
            if (trimmed.StartsWith("#"))
            {
                FlushParagraph();
                int level = 0;
                while (level < trimmed.Length && trimmed[level] == '#') level++;
                string text = trimmed.Substring(level).Trim();
                if (level == 1 && title == "Dama - Week 13") title = text;
                int clamped = Mathf.Clamp(level, 1, 4);
                body.AppendLine($"<h{clamped}>{Inline(text)}</h{clamped}>");
                index++;
                continue;
            }

            // Figure image
            Match image = Regex.Match(trimmed, @"^!\[(.*?)\]\((.*?)\)$");
            if (image.Success)
            {
                FlushParagraph();
                body.AppendLine(
                    $"<figure><img src=\"{Escape(image.Groups[2].Value)}\" alt=\"{Escape(image.Groups[1].Value)}\"/>" +
                    $"<figcaption>{Inline(image.Groups[1].Value)}</figcaption></figure>");
                index++;
                continue;
            }

            // Bullet list
            if (trimmed.StartsWith("- "))
            {
                FlushParagraph();
                body.AppendLine("<ul>");
                while (index < lines.Length && lines[index].Trim().StartsWith("- "))
                {
                    string item = lines[index].Trim().Substring(2).Trim();
                    index++;
                    item = AbsorbWrappedLines(lines, ref index, item);
                    body.AppendLine("<li>" + Inline(item) + "</li>");
                }
                body.AppendLine("</ul>");
                continue;
            }

            // Numbered list
            if (Regex.IsMatch(trimmed, @"^\d+\.\s"))
            {
                FlushParagraph();
                body.AppendLine("<ol>");
                while (index < lines.Length && Regex.IsMatch(lines[index].Trim(), @"^\d+\.\s"))
                {
                    string item = Regex.Replace(lines[index].Trim(), @"^\d+\.\s", "");
                    index++;
                    item = AbsorbWrappedLines(lines, ref index, item);
                    body.AppendLine("<li>" + Inline(item) + "</li>");
                }
                body.AppendLine("</ol>");
                continue;
            }

            // Blank line ends the current paragraph
            if (trimmed.Length == 0)
            {
                FlushParagraph();
                index++;
                continue;
            }

            // Key/value header lines (**Game:** Dama) are one line each
            if (inHeaderBlock && Regex.IsMatch(trimmed, @"^\*\*[^*]+:\*\*"))
            {
                FlushParagraph();
                body.AppendLine("<p class=\"meta\">" + Inline(trimmed) + "</p>");
                index++;
                continue;
            }

            paragraph.Add(trimmed);
            index++;
        }

        FlushParagraph();
        return Wrap(title, body.ToString());
    }

    /// <summary>
    /// A list item in the documents is often wrapped over several indented lines. Those
    /// continuation lines belong to the item, not to a new paragraph, so they are folded
    /// back in until a blank line or an unindented block starts.
    /// </summary>
    private static string AbsorbWrappedLines(string[] lines, ref int index, string item)
    {
        StringBuilder text = new StringBuilder(item);

        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.Trim().Length == 0) break;          // blank line ends the list
            if (line.Length == 0 || !char.IsWhiteSpace(line[0])) break;   // unindented = new block
            if (line.Trim().StartsWith("- ")) break;     // next bullet
            if (Regex.IsMatch(line.Trim(), @"^\d+\.\s")) break;        // next number

            text.Append(' ').Append(line.Trim());
            index++;
        }

        return text.ToString();
    }

    private static void AppendTable(StringBuilder body, List<string> rows)
    {
        List<List<string>> cells = new List<List<string>>();
        bool sawSeparator = false;

        foreach (string row in rows)
        {
            if (!sawSeparator && Regex.IsMatch(row, @"^\|[\s\-:|]+\|$"))
            {
                sawSeparator = true;
                continue;
            }

            string inner = row.Trim();
            if (inner.StartsWith("|")) inner = inner.Substring(1);
            if (inner.EndsWith("|")) inner = inner.Substring(0, inner.Length - 1);

            List<string> split = new List<string>();
            foreach (string cell in inner.Split('|')) split.Add(cell.Trim());
            cells.Add(split);
        }

        if (cells.Count == 0) return;

        body.AppendLine("<table>");
        body.AppendLine("<thead><tr>");
        foreach (string cell in cells[0]) body.AppendLine("<th>" + Inline(cell) + "</th>");
        body.AppendLine("</tr></thead>");
        body.AppendLine("<tbody>");

        for (int r = 1; r < cells.Count; r++)
        {
            body.AppendLine("<tr>");
            for (int c = 0; c < cells[0].Count; c++)
            {
                string value = c < cells[r].Count ? cells[r][c] : "";
                body.AppendLine("<td>" + Inline(value) + "</td>");
            }
            body.AppendLine("</tr>");
        }

        body.AppendLine("</tbody></table>");
    }

    private static string Inline(string text)
    {
        string output = Escape(text);
        output = Regex.Replace(output, "`([^`]+)`", "<code>$1</code>");
        output = Regex.Replace(output, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");
        output = Regex.Replace(output, @"\*([^*]+)\*", "<em>$1</em>");
        return output;
    }

    private static string Escape(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static string Wrap(string title, string body)
    {
        return "<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\"/>\n" +
               $"<title>{Escape(title)}</title>\n<style>\n" + Css + "\n</style>\n</head>\n<body>\n" +
               body + "\n</body>\n</html>\n";
    }

    private const string Css = @"
@page { size: A4; margin: 17mm 15mm 18mm 15mm; }

html { -webkit-print-color-adjust: exact; print-color-adjust: exact; }

body {
  font-family: 'Segoe UI', 'Calibri', Arial, sans-serif;
  font-size: 10.5pt;
  line-height: 1.4;
  color: #14181d;
  margin: 0;
  max-width: 100%;
}

h1 {
  font-size: 19pt;
  line-height: 1.2;
  margin: 0 0 10pt 0;
  padding-bottom: 6pt;
  border-bottom: 2.5pt solid #14181d;
  color: #0d1116;
}

h2 {
  font-size: 13.5pt;
  margin: 15pt 0 6pt 0;
  padding-bottom: 3pt;
  border-bottom: 0.8pt solid #b9c2cc;
  color: #0d1116;
  page-break-after: avoid;
}

h3 { font-size: 11.5pt; margin: 14pt 0 5pt 0; color: #1c2833; page-break-after: avoid; }
h4 { font-size: 10.5pt; margin: 12pt 0 4pt 0; color: #1c2833; page-break-after: avoid; }

p { margin: 0 0 6pt 0; orphans: 3; widows: 3; }

p.meta {
  margin: 0 0 2.5pt 0;
  font-size: 9.5pt;
  color: #2c3e50;
}

p.meta strong { color: #0d1116; }

hr {
  border: 0;
  border-top: 0.8pt solid #ccd4dc;
  margin: 11pt 0;
}

ul, ol { margin: 0 0 8pt 0; padding-left: 17pt; }
li { margin-bottom: 3pt; }

code {
  font-family: 'Consolas', 'Cascadia Mono', monospace;
  font-size: 9pt;
  background: #eef1f4;
  padding: 0.5pt 3pt;
  border-radius: 2pt;
  color: #1a2a38;
}

pre {
  background: #f5f7f9;
  border: 0.6pt solid #d3dae1;
  border-left: 2.5pt solid #4a6b8a;
  padding: 8pt 10pt;
  margin: 0 0 10pt 0;
  overflow-x: auto;
  page-break-inside: avoid;
}

pre code { background: none; padding: 0; font-size: 8.5pt; line-height: 1.42; }

table {
  width: 100%;
  border-collapse: collapse;
  margin: 0 0 11pt 0;
  font-size: 9pt;
  page-break-inside: auto;
}

thead { display: table-header-group; }

th {
  background: #e7ecf1;
  text-align: left;
  font-weight: 600;
  color: #0d1116;
  border: 0.6pt solid #c3ccd5;
  padding: 4.5pt 5pt;
}

td {
  border: 0.6pt solid #d3dae1;
  padding: 4.5pt 5pt;
  vertical-align: top;
}

tbody tr:nth-child(even) td { background: #fafbfc; }
tr { page-break-inside: avoid; }

figure { margin: 10pt 0 12pt 0; text-align: center; page-break-inside: avoid; }
figure img { max-width: 100%; height: auto; border: 0.6pt solid #d3dae1; }
figcaption { font-size: 8.5pt; color: #5a6b7b; margin-top: 4pt; font-style: italic; }

strong { color: #0d1116; }
";
}
