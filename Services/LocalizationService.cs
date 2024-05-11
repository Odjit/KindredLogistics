using ProjectM;
using Stunlock.Core;
using Stunlock.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Unity.Entities.UniversalDelegates;

namespace KindredLogistics.Services
{
    internal class LocalizationService
    {
        struct Code
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public string Description { get; set; }
        }

        struct Node
        {
            public string Guid { get; set; }
            public string Text { get; set; }
        }

        struct LocalizationFile
        {
            public Code[] Codes { get; set; }
            public Node[] Nodes { get; set; }
        }

        Dictionary<string, string> localization = [];
        Dictionary<int, string> itemNames = [];

        public LocalizationService()
        {
            LoadLocalization();
            LoadItemNames();
        }

        void LoadLocalization()
        {
            var resourceName = "KindredLogistics.Localization.English.json";

            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    string jsonContent = reader.ReadToEnd();
                    var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);
                    localization = localizationFile.Nodes.ToDictionary(x => x.Guid, x => x.Text);
                }
            }
            else
            {
                Console.WriteLine("Resource not found!");
            }
        }

        void LoadItemNames()
        {
            var resourceName = "KindredLogistics.Data.ItemNames.json";

            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    string jsonContent = reader.ReadToEnd();
                    itemNames = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);
                }
            }
            else
            {
                Console.WriteLine("Resource not found!");
            }
        }

        public string GetLocalization(string guid)
        {
            if (localization.TryGetValue(guid, out var text))
            {
                return text;
            }
            return "<Localization not found!>";
        }

        public string GetLocalization(LocalizationKey key)
        {
            var guid = key.Key.ToGuid().ToString();
            return GetLocalization(guid);
        }

        public string GetItemName(PrefabGUID itemPrefabGUID)
        {
            if(!itemNames.TryGetValue(itemPrefabGUID._Value, out var itemLocalizationHash))
            {
                return null;
            }
            return GetLocalization(itemLocalizationHash);
        }

    }
}
