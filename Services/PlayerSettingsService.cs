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
            public bool AutoStash { get; set; }
            public bool AutoPull { get; set; }
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

        public bool GetAutoStash(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                return false;
            return settings.AutoStash;
        }

        public void SetAutoStash(ulong playerId, bool value)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.AutoStash = value;
            playerSettings[playerId] = settings;
            SaveSettings();
        }

        public bool ToggleAutoStash(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.AutoStash = !settings.AutoStash;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.AutoStash;
        }

        public bool GetAutoPull(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                return false;
            return settings.AutoPull;
        }

        public void SetAutoPull(ulong playerId, bool value)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.AutoPull = value;
            playerSettings[playerId] = settings;
            SaveSettings();
        }

        public bool ToggleAutoPull(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.AutoPull = !settings.AutoPull;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.AutoPull;
        }

        public bool GetAutoStashMissions(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                return false;
            return settings.AutoStashMissions;
        }

        public void SetAutoStashMissions(ulong playerId, bool value)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.AutoStashMissions = value;
            playerSettings[playerId] = settings;
            SaveSettings();
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
