using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VampireCommandFramework;

namespace KindredLogistics.Commands.Converters;

public record struct FoundItem(PrefabGUID prefab);

class FoundItemConverter : CommandArgumentConverter<FoundItem>
{
    public override FoundItem Parse(ICommandContext ctx, string input)
    {
        if (TryGet(input, out var result)) return result;

        List<PrefabGUID> searchResults = [];
        foreach (var kvp in itemNamesToPrefabs)
        {
            if (kvp.Key.Contains(input, StringComparison.OrdinalIgnoreCase))
            {
                searchResults.Add(kvp.Value);
            }
        }

        if (searchResults.Count == 1)
        {
            return new FoundItem(searchResults[0]);
        }

        var lengthOfFail = 60 + "\n...".Length;

        if (searchResults.Count > 1)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Multiple results be more specific");
            foreach (var prefab in searchResults)
            {
                var name = prefab.PrefabName();
                if (sb.Length + name.Length + lengthOfFail >= Core.MAX_REPLY_LENGTH)
                {
                    sb.AppendLine("...");
                    throw ctx.Error(sb.ToString());
                }
                else
                {
                    sb.AppendLine(name);
                }
            }
            throw ctx.Error(sb.ToString());
        }

        // Try a double search splitting the input
        for (var i = 3; i < input.Length; ++i)
        {
            var inputOne = input[..i];
            var inputTwo = input[i..];
            foreach (var kvp in itemNamesToPrefabs)
            {
                if (kvp.Key.Contains(inputOne, StringComparison.OrdinalIgnoreCase) &&
                    kvp.Key.Contains(inputTwo, StringComparison.OrdinalIgnoreCase))
                {
                    searchResults.Add(kvp.Value);
                }
            }

            if (searchResults.Count == 1)
            {
                return new FoundItem(searchResults[0]);
            }
        }

        if (searchResults.Count > 1)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Multiple results be more specific");
            foreach (var prefab in searchResults)
            {
                var name = prefab.PrefabName();
                if (sb.Length + name.Length + lengthOfFail >= Core.MAX_REPLY_LENGTH)
                {
                    sb.AppendLine("...");
                    throw ctx.Error(sb.ToString());
                }
                else
                {
                    sb.AppendLine(name);
                }
            }
            throw ctx.Error(sb.ToString());
        }

        throw ctx.Error($"No items found matching: {input}");
    }

    static Dictionary<string, PrefabGUID> itemNamesToPrefabs = new Dictionary<string, PrefabGUID>(StringComparer.OrdinalIgnoreCase);

    public static void LoadItemNames()
    {
        foreach (var (prefab, name) in Core.PrefabCollectionSystem.PrefabGuidToNameDictionary)
        {
            if(name.StartsWith("Item_"))
                itemNamesToPrefabs[prefab.PrefabName()] = prefab;
        }
    }

    private static bool TryGet(string input, out FoundItem item)
    {
        if (itemNamesToPrefabs.TryGetValue(input, out var prefab))
        {
            item = new FoundItem(prefab);
            return true;
        }

        item = new FoundItem(new(0));
        return false;
    }
}
