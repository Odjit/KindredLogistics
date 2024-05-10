using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KindredLogistics.Services
{
    internal class PlayerSettingsService
    {
        static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
        static readonly string PLAYER_SETTINGS_PATH = Path.Combine(CONFIG_PATH, "playerSettings.json");

        private static readonly JsonSerializerOptions prettyJsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };

        public struct PlayerSettings
        {
            public bool SortStash { get; set; }
            public bool CraftPull { get; set; }
            public bool AutoStashMissions { get; set; }
        }

        Dictionary<ulong, PlayerSettings> playerSettings = [];

        public PlayerSettingsService()
        {
            LoadSettings();
        }

        void LoadSettings()
        {
            if(!File.Exists(PLAYER_SETTINGS_PATH))
            {
                SaveSettings();
                return;
            }

            var json = File.ReadAllText(PLAYER_SETTINGS_PATH);
            playerSettings = JsonSerializer.Deserialize<Dictionary<ulong, PlayerSettings>>(json);
        }

        void SaveSettings()
        {
            if (!Directory.Exists(CONFIG_PATH))
                Directory.CreateDirectory(CONFIG_PATH);
            var json = JsonSerializer.Serialize(playerSettings, prettyJsonOptions);
            File.WriteAllText(PLAYER_SETTINGS_PATH, json);
        }

        public bool IsSortStashEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                return false;
            return settings.SortStash;
        }

        public bool ToggleSortStash(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.SortStash = !settings.SortStash;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.SortStash;
        }

        public bool GetCraftPull(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                return false;
            return settings.CraftPull;
        }

        public bool ToggleCraftPull(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.CraftPull = !settings.CraftPull;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.CraftPull;
        }

        public bool GetAutoStashMissions(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                return false;
            return settings.AutoStashMissions;
        }

        public bool ToggleAutoStashMissions(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.AutoStashMissions = !settings.AutoStashMissions;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.AutoStashMissions;
        }

        public PlayerSettings GetSettings(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                return new PlayerSettings();
            return settings;
        }
    }
}
