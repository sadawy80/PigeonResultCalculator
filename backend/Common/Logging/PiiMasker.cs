using System.Text.RegularExpressions;

namespace PRC.Common.Logging;

/// <summary>
/// Best-effort scrubber for personally-identifiable information that should never
/// appear in structured logs. Used both as a Serilog destructuring policy and as a
/// helper for explicit masking in service code.
/// </summary>
public static class PiiMasker
{
    private static readonly Regex EmailRegex = new(
        @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // International phone — 7-15 digits with optional +, spaces, dashes, parens.
    private static readonly Regex PhoneRegex = new(
        @"(?<![\w-])\+?\d[\d\s\-().]{6,18}\d(?![\w-])",
        RegexOptions.Compiled);

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "passwordhash", "pwd", "secret", "token", "accesstoken",
        "refreshtoken", "authorization", "apikey", "api_key", "bearer",
        "email", "emailaddress", "phone", "phonenumber", "mobile",
        "firstname", "lastname", "fullname", "ssn", "nationalid"
    };

    public static bool IsSensitiveKey(string propertyName)
        => SensitiveKeys.Contains(propertyName);

    public static string MaskEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var at = value.IndexOf('@');
        if (at <= 0) return "***";
        var local = value[..at];
        var domain = value[(at + 1)..];
        var keep = Math.Min(2, local.Length);
        return $"{local[..keep]}***@{domain}";
    }

    public static string Scrub(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        input = EmailRegex.Replace(input, m => MaskEmail(m.Value));
        input = PhoneRegex.Replace(input, "[redacted-phone]");
        return input;
    }
}
