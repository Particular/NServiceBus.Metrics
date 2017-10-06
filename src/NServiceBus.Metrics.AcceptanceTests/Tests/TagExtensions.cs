using System;
using System.Linq;
using System.Collections.Generic;

static class TagExtensions
{
    public static string GetTagValue(this IEnumerable<string> tags, string key)
    {
        if (!TryGetTagValue(tags, key, out var result))
        {
            throw new Exception($"Tag {key} not found.");
        }
        return result;
    }

    public static bool TryGetTagValue(this IEnumerable<string> tags, string key, out string value)
    {
        value = tags
            .Where(t => t.StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Substring(key.Length + 1))
            .FirstOrDefault();

        return value != null;
    }
}
