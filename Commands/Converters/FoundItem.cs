using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Text;
using VampireCommandFramework;

namespace KindredLogistics.Commands.Converters;

public record struct FoundItem(PrefabGUID prefab);

class FoundItemConverter : CommandArgumentConverter<FoundItem>
{
    public override FoundItem Parse(ICommandContext ctx, string input)
    {
        string[] split = input.Split(":");
        input = split[0];

        if (TryGet(input, out var result)) return result;

        List<PrefabGUID> searchResults = [];
        foreach (var kvp in itemNamesToPrefabs)
        {
            if (kvp.Key.Contains(input, StringComparison.OrdinalIgnoreCase))
            {
                searchResults.Add(kvp.Value);
            }
        }


        if (searchResults.Count == 0)
        {
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
            }
        }

        if (searchResults.Count == 0)
        {
            // Try a triple search splitting the input
            foreach (var kvp in itemNamesToPrefabs)
            {
                for (var i = 3; i < input.Length - 3; ++i)
                {
                    var inputOne = input[..i];
                    if (!kvp.Key.Contains(inputOne, StringComparison.OrdinalIgnoreCase)) continue;

                    for (var j = i + 3; j < input.Length; j++)
                    {
                        var inputTwo = input[i..j];
                        var inputThree = input[j..];

                        if (kvp.Key.Contains(inputTwo, StringComparison.OrdinalIgnoreCase) &&
                            kvp.Key.Contains(inputThree, StringComparison.OrdinalIgnoreCase))
                        {
                            searchResults.Add(kvp.Value);
                        }
                    }
                }
            }
        }

        if (searchResults.Count == 1)
        {
            return new FoundItem(searchResults[0]);
        }

        // if a index is given try to return it with the index
        if (searchResults.Count > 1 && split.Length == 2)
        {
            int number;
            if (!int.TryParse(split[1], out number))
            {
                throw ctx.Error("Could not covnert index: " + split[1] + " to a number");
            }
            if ((number - 1) > searchResults.Count || (number - 1) < 0)
            {
                throw ctx.Error("Index " + number + " is out of bounds for search result size " + searchResults.Count);
            }
            return new FoundItem(searchResults[number - 1]);
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

        throw ctx.Error($"No items found matching: {input}");
    }

    static Dictionary<string, PrefabGUID> itemNamesToPrefabs = new Dictionary<string, PrefabGUID>(StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<PrefabGUID> skipItems = [
        new PrefabGUID(-625033436), // Chest TransmogTest
        new PrefabGUID(1217578824), // Legs TransmogTest
        new PrefabGUID(409678749),  // Item_Headgear_GeneralHelmet 
        new PrefabGUID(2029158532), // Item_Dummy_Rat
        new PrefabGUID(-1199259626),// Item_Ingredient_Scales
        new PrefabGUID(930747930),  // Item_Dummy_Silkworm
        ];

    public static void LoadItemNames()
    {
        foreach (var (prefab, name) in Core.PrefabCollectionSystem.PrefabGuidToNameDictionary)
        {
            if(skipItems.Contains(prefab)) continue;
            if (name.StartsWith("Item_") && !name.EndsWith("_Base") && !name.EndsWith("_Trader_Template") && !name.EndsWith("_Debug"))
            {
                var prefabName = prefab.PrefabName();
                /*if(itemNamesToPrefabs.TryGetValue(prefabName, out var otherPrefab))
                {
                    Core.Log.LogWarning($"Duplicate item name found: {prefabName} {prefab} {otherPrefab}");
                }//*/
                itemNamesToPrefabs[prefabName] = prefab;
            }
        }
    }

    static bool TryGet(string input, out FoundItem item)
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
