using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace KindredLogistics.Services
{
    internal class PlayerSettingsService
    {
        const int GLOBAL_PLAYER_ID = 0;

        static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
        static readonly string PLAYER_SETTINGS_PATH = Path.Combine(CONFIG_PATH, "playerSettings.json");

        static readonly JsonSerializerOptions prettyJsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };

        public struct PlayerSettings
        {
            public PlayerSettings()
            {
                DontPullLast = true;
            }

            public bool SortStash { get; set; }
            public bool Pull { get; set; }
            public bool CraftPull { get; set; }
            public bool DontPullLast { get; set; }
            public bool AutoStashMissions { get; set; }
            public bool Conveyor { get; set; }
            public bool Salvage { get; set; }
            public bool UnitSpawner { get; set; }
            public bool Brazier { get; set; }
            public bool Named { get; set; }
            public bool SilentPull { get; set; }
            public bool SilentStash { get; set; }
        }

        PlayerSettings defaultSettings = new();

        Dictionary<ulong, PlayerSettings> playerSettings = [];

        public PlayerSettingsService()
        {
            LoadSettings();

            if(!playerSettings.ContainsKey(GLOBAL_PLAYER_ID))
            {
                playerSettings[GLOBAL_PLAYER_ID] = new PlayerSettings()
                {
                    SortStash = true,
                    Pull = true,
                    CraftPull = true,
                    AutoStashMissions = true,
                    Conveyor = true,
                    Salvage = true,
                    UnitSpawner = false,
                    Brazier = false,
                    Named = false,
                };
                SaveSettings();
            }
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
                settings = defaultSettings;
            return settings.SortStash && playerSettings[GLOBAL_PLAYER_ID].SortStash;
        }

        public bool ToggleSortStash(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.SortStash = !settings.SortStash;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.SortStash;
        }
        
        public bool TogglePull()
        {
            if (!playerSettings.TryGetValue(GLOBAL_PLAYER_ID, out var settings))
                settings = new PlayerSettings();
            settings.Pull = !settings.Pull;
            playerSettings[GLOBAL_PLAYER_ID] = settings;
            SaveSettings();
            return settings.Pull;
        }

        public bool IsPullEnabled()
        {
            return !playerSettings[GLOBAL_PLAYER_ID].Pull;
        }

        public bool IsCraftPullEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.CraftPull && playerSettings[GLOBAL_PLAYER_ID].CraftPull;
        }

        public bool ToggleCraftPull(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.CraftPull = !settings.CraftPull;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.CraftPull;
        }

        public bool IsDontPullLastEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.DontPullLast;
        }

        public bool ToggleDontPullLast(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.DontPullLast = !settings.DontPullLast;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.DontPullLast;
        }

        public bool IsAutoStashMissionsEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.AutoStashMissions && playerSettings[GLOBAL_PLAYER_ID].AutoStashMissions;
        }

        public bool ToggleAutoStashMissions(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.AutoStashMissions = !settings.AutoStashMissions;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.AutoStashMissions;
        }

        public bool IsConveyorEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.Conveyor && playerSettings[GLOBAL_PLAYER_ID].Conveyor;
        }

        public bool IsSalvageEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.Salvage && playerSettings[GLOBAL_PLAYER_ID].Salvage;
        }

        public bool ToggleSalvage(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.Salvage = !settings.Salvage;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.Salvage;
        }

        public bool IsUnitSpawnerEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.UnitSpawner;
        }

        public bool ToggleUnitSpawner(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.UnitSpawner = !settings.UnitSpawner;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.UnitSpawner;
        }
        
        public bool IsBrazierEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.Brazier;
        }

        public bool IsSolarEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.Named;
        }

        public bool ToggleSolar(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.Named = !settings.Named;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.Named;
        }

        public bool ToggleBrazier(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.Brazier = !settings.Brazier;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.Brazier;
        }

        public bool ToggleSilentPull(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.SilentPull = !settings.SilentPull;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.SilentPull;
        }

        public bool IsSilentPullEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.SilentPull;
        }

        public bool ToggleSilentStash(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.SilentStash = !settings.SilentStash;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.SilentStash;
        }

        public bool IsSilentStashEnabled(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = defaultSettings;
            return settings.SilentStash;
        }

        public bool ToggleConveyor(ulong playerId = GLOBAL_PLAYER_ID)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                settings = new PlayerSettings();
            settings.Conveyor = !settings.Conveyor;
            playerSettings[playerId] = settings;
            SaveSettings();
            return settings.Conveyor;
        }

        public PlayerSettings GetSettings(ulong playerId)
        {
            if (!playerSettings.TryGetValue(playerId, out var settings))
                return new PlayerSettings();
            return settings;
        }

        public PlayerSettings GetGlobalSettings()
        {
            return playerSettings[GLOBAL_PLAYER_ID];
        }
    }
}
