using Serilog.Core;
using Serilog.Events;

namespace PRC.Common.Logging;

/// <summary>
/// Serilog destructuring policy that masks sensitive property values when they
/// appear in structured log payloads. Plug it in via
/// <c>.Destructure.With&lt;PiiDestructuringPolicy&gt;()</c> on the
/// <c>LoggerConfiguration</c>.
/// </summary>
public class PiiDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory, out LogEventPropertyValue? result)
    {
        if (value is null) { result = null; return false; }

        var type = value.GetType();
        if (!type.IsClass || type == typeof(string))
        {
            result = null;
            return false;
        }

        var props = type.GetProperties();
        if (props.Length == 0) { result = null; return false; }

        var members = new List<LogEventProperty>(props.Length);
        foreach (var p in props)
        {
            if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;

            object? raw;
            try { raw = p.GetValue(value); }
            catch { continue; }

            object? masked = MaskIfSensitive(p.Name, raw);
            members.Add(new LogEventProperty(p.Name, factory.CreatePropertyValue(masked, true)));
        }

        result = new StructureValue(members, type.Name);
        return true;
    }

    private static object? MaskIfSensitive(string propertyName, object? raw)
    {
        if (raw is null) return null;

        if (!PiiMasker.IsSensitiveKey(propertyName)) return raw;

        if (raw is string s)
        {
            return propertyName.Contains("email", StringComparison.OrdinalIgnoreCase)
                ? PiiMasker.MaskEmail(s)
                : "***";
        }
        return "***";
    }
}
