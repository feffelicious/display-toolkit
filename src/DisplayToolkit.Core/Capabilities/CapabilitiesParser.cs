using System.Globalization;

namespace DisplayToolkit.Core.Capabilities;

/// <summary>
/// Parses MCCS capabilities strings such as
/// <c>(prot(monitor)type(LCD)model(PG32UCWM)vcp(02 10 12 14(05 08 0B) FD(6979))mccs_ver(2.2))</c>.
/// </summary>
/// <remarks>
/// Real monitors are sloppy: missing outer or closing parentheses, codes without separating spaces and trailing
/// garbage all occur in the wild. The parser is lenient and never throws on bad input; it keeps what it understood.
/// </remarks>
public static class CapabilitiesParser
{
    public static MonitorCapabilities Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return MonitorCapabilities.Empty;
        }

        var sections = ParseSections(raw);
        var vcp = sections.TryGetValue("vcp", out var vcpBody) ? ParseVcp(vcpBody) : new Dictionary<byte, IReadOnlyList<uint>>();

        return new MonitorCapabilities(
            raw,
            sections.GetValueOrDefault("model")?.Trim(),
            sections.GetValueOrDefault("mccs_ver")?.Trim(),
            vcp);
    }

    /// <summary>Splits the string into its top-level <c>name(body)</c> sections.</summary>
    private static Dictionary<string, string> ParseSections(string raw)
    {
        var text = raw.Trim();
        if (text.StartsWith('('))
        {
            text = text[1..];
        }

        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var position = 0;
        while (position < text.Length)
        {
            var nameStart = position;
            while (position < text.Length && (char.IsAsciiLetterOrDigit(text[position]) || text[position] == '_'))
            {
                position++;
            }

            if (position == nameStart || position >= text.Length || text[position] != '(')
            {
                position = Math.Max(position, nameStart + 1);
                continue;
            }

            var name = text[nameStart..position];
            var body = ReadBalanced(text, ref position);
            sections.TryAdd(name, body);
        }
        return sections;
    }

    /// <summary>Reads from an opening parenthesis to its match (or the end of the text) and returns the inside.</summary>
    private static string ReadBalanced(string text, ref int position)
    {
        var start = position + 1;
        var depth = 0;
        for (; position < text.Length; position++)
        {
            if (text[position] == '(')
            {
                depth++;
            }
            else if (text[position] == ')' && --depth == 0)
            {
                position++;
                return text[start..(position - 1)];
            }
        }
        return text[Math.Min(start, text.Length)..];
    }

    private static Dictionary<byte, IReadOnlyList<uint>> ParseVcp(string body)
    {
        var result = new Dictionary<byte, IReadOnlyList<uint>>();
        var position = 0;
        byte? lastCode = null;

        while (position < body.Length)
        {
            var c = body[position];
            if (char.IsWhiteSpace(c))
            {
                position++;
            }
            else if (c == '(')
            {
                var values = ParseValues(ReadBalanced(body, ref position));
                if (lastCode is { } code)
                {
                    result[code] = values;
                }
            }
            else if (position + 1 < body.Length && char.IsAsciiHexDigit(c) && char.IsAsciiHexDigit(body[position + 1]))
            {
                // VCP codes are always exactly two hex digits, even when monitors omit the separating spaces.
                var code = byte.Parse(body.AsSpan(position, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                result.TryAdd(code, []);
                lastCode = code;
                position += 2;
            }
            else
            {
                position++;
            }
        }
        return result;
    }

    private static uint[] ParseValues(string body)
    {
        var values = new List<uint>();
        foreach (var token in body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            // Tokens are normally 2 or 4 hex digits. A longer run means the spaces were left out: split into bytes.
            if (token.Length > 4 && token.Length % 2 == 0)
            {
                for (var i = 0; i < token.Length; i += 2)
                {
                    AddHex(values, token.AsSpan(i, 2));
                }
            }
            else
            {
                AddHex(values, token);
            }
        }
        return [.. values];
    }

    private static void AddHex(List<uint> values, ReadOnlySpan<char> token)
    {
        if (uint.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            values.Add(value);
        }
    }
}
