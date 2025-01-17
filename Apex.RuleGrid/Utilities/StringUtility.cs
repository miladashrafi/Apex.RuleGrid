using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apex.RuleGrid.Utilities;

public static class StringUtility
{
    public static string FirstCharToUpperAsSpan(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        Span<char> destination = stackalloc char[1];
        input.AsSpan(0, 1).ToUpperInvariant(destination);
        return $"{destination}{input.AsSpan(1)}";
    }

    public static string SerializeForLog(this object input)
    {
        if (input is null)
            return "";

        return JsonSerializer.Serialize(input, input.GetType(), new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 10,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });
    }
}