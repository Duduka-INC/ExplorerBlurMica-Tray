using System.Text;

namespace ExplorerBlurMicaTray.Core;

/// <summary>
/// A minimal INI file that keeps every line it does not touch: comments, unknown keys and their
/// order survive a round trip. Section and key names are case-insensitive.
/// </summary>
internal sealed class IniDocument
{
    private readonly List<Line> _lines = [];

    private sealed record Line(string Text, string? Section, string? Key, string? Value);

    public static IniDocument Parse(string text)
    {
        var doc = new IniDocument();
        string? section = null;
        foreach (string raw in text.ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = raw.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                section = trimmed[1..^1].Trim();
                doc._lines.Add(new Line(raw, section, null, null));
            }
            else if (section is not null && !trimmed.StartsWith(';') && !trimmed.StartsWith('#') && trimmed.IndexOf('=') is > 0 and var eq)
            {
                doc._lines.Add(new Line(raw, section, trimmed[..eq].Trim(), trimmed[(eq + 1)..].Trim()));
            }
            else
            {
                doc._lines.Add(new Line(raw, section, null, null));
            }
        }

        // Split leaves an empty entry after a trailing newline; ToString adds it back.
        if (doc._lines.Count > 0 && doc._lines[^1].Text.Length == 0)
            doc._lines.RemoveAt(doc._lines.Count - 1);
        return doc;
    }

    public string? Get(string section, string key) =>
        _lines.FirstOrDefault(l => l.Key is not null && Same(l.Section, section) && Same(l.Key, key))?.Value;

    public void Set(string section, string key, string value)
    {
        int existing = _lines.FindIndex(l => l.Key is not null && Same(l.Section, section) && Same(l.Key, key));
        if (existing >= 0)
        {
            string originalKey = _lines[existing].Key!;
            _lines[existing] = new Line($"{originalKey}={value}", _lines[existing].Section, originalKey, value);
            return;
        }

        var line = new Line($"{key}={value}", section, key, value);

        int lastInSection = _lines.FindLastIndex(l => Same(l.Section, section) && (l.Key is not null || IsHeader(l)));
        if (lastInSection < 0)
        {
            _lines.Add(new Line($"[{section}]", section, null, null));
            _lines.Add(line);
        }
        else
        {
            _lines.Insert(lastInSection + 1, line);
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var line in _lines)
            sb.Append(line.Text).Append("\r\n");
        return sb.ToString();
    }

    private static bool IsHeader(Line line) => line.Key is null && line.Text.TrimStart().StartsWith('[');

    private static bool Same(string? a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
