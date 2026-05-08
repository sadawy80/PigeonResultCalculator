using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PRC.RenderingService.Services;

public static class TemplateRenderer
{
    private static readonly Regex SimpleVar = new(@"\{\{([^#/][^}]*?)\}\}", RegexOptions.Compiled);
    private static readonly Regex EachBlock = new(@"\{\{#each\s+(\w+)\}\}(.*?)\{\{/each\}\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex IfBlock   = new(@"\{\{#if\s+([^}]+)\}\}(.*?)(\{\{else\}\}(.*?))?\{\{/if\}\}", RegexOptions.Compiled | RegexOptions.Singleline);

    public static string Render(string template, object data)
    {
        var dict = ToDict(data);

        string result = EachBlock.Replace(template, m =>
        {
            var arrayKey = m.Groups[1].Value.Trim();
            var innerTemplate = m.Groups[2].Value;

            if (dict.TryGetValue(arrayKey, out var arrObj) && arrObj is List<object> items)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] is Dictionary<string, object> itemDict)
                    {
                        itemDict["@index"] = (object)(long)i;
                        itemDict["@first"] = (object)(i == 0);
                        itemDict["@last"]  = (object)(i == items.Count - 1);
                        itemDict["isFirst"] = (object)(i == 0);
                        sb.Append(RenderSimple(innerTemplate, itemDict));
                    }
                }
                return sb.ToString();
            }
            return string.Empty;
        });

        result = IfBlock.Replace(result, m =>
        {
            var condition = m.Groups[1].Value.Trim();
            var truePart  = m.Groups[2].Value;
            var falsePart = m.Groups[4].Value;
            bool isTrue = EvaluateCondition(condition, dict);
            return RenderSimple(isTrue ? truePart : falsePart, dict);
        });

        result = RenderSimple(result, dict);
        return result;
    }

    private static string RenderSimple(string template, Dictionary<string, object> dict) =>
        SimpleVar.Replace(template, m =>
        {
            var path = m.Groups[1].Value.Trim();
            var value = ResolvePath(path, dict);
            return value?.ToString() ?? string.Empty;
        });

    private static bool EvaluateCondition(string condition, Dictionary<string, object> dict)
    {
        var eqMatch = Regex.Match(condition, @"\(eq\s+(.+?)\s+""([^""]+)""\)");
        if (eqMatch.Success)
        {
            var left = ResolvePath(eqMatch.Groups[1].Value.Trim(), dict)?.ToString() ?? "";
            return left == eqMatch.Groups[2].Value;
        }
        var value = ResolvePath(condition, dict);
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is long l) return l != 0;
        if (value is string s) return !string.IsNullOrEmpty(s);
        return true;
    }

    private static object? ResolvePath(string path, Dictionary<string, object> dict)
    {
        var parts = path.Split('.');
        object? current = dict;
        foreach (var part in parts)
        {
            var key = part.Trim('[', ']');
            if (current is Dictionary<string, object> d)
            {
                if (!d.TryGetValue(key, out current)) return null;
            }
            else if (current is List<object> list)
            {
                if (int.TryParse(key, out int idx) && idx < list.Count)
                    current = list[idx];
                else
                    return null;
            }
            else
                return null;
        }
        return current;
    }

    public static Dictionary<string, object> ToDict(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        return ParseJsonObject(JsonDocument.Parse(json).RootElement);
    }

    private static Dictionary<string, object> ParseJsonObject(JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in element.EnumerateObject())
            dict[prop.Name] = ParseValue(prop.Value);
        return dict;
    }

    private static object ParseValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => ParseJsonObject(element),
        JsonValueKind.Array  => element.EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Object
                ? (object)ParseJsonObject(e)
                : ParseValue(e))
            .ToList<object>(),
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
        JsonValueKind.True   => (object)true,
        JsonValueKind.False  => (object)false,
        _                    => (object)""
    };
}
