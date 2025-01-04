using KindredLogistics;
using KindredLogistics.Commands.Converters;
using KindredLogistics.Services;
using VampireCommandFramework;

namespace Logistics.Commands
{
    [CommandGroup(name: "logistics", "l")]
    public static class LogisticsCommands
    {
        [Command(name: "sortstash", shortHand: "ss", usage: ".l ss", description: "Toggles autostashing on double clicking sort button for player.")]
        public static void TogglePlayerAutoStash(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoStash = Core.PlayerSettings.ToggleSortStash(SteamID);
            ctx.Reply($"SortStash is {(autoStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }
       
        [Command(name: "craftpull", shortHand: "cr", usage: ".l cr", description: "Toggles right-clicking on recipes for missing ingredients.")]
        public static void TogglePlayerAutoPull(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoPull = Core.PlayerSettings.ToggleCraftPull(SteamID);
            ctx.Reply($"CraftPull is {(autoPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "dontpulllast", shortHand: "dpl", usage: ".l dpl", description: "Toggles the ability to not pull the last item from a container for Logistics commands.")]
        public static void ToggleDontPullLast(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var dontPullLast = Core.PlayerSettings.ToggleDontPullLast(SteamID);
            ctx.Reply($"DontPullLast is {(dontPullLast ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "autostashmissions", shortHand: "asm", usage: ".l asm", description: "Toggles autostashing for servant missions.")]
        public static void ToggleServantAutoStash(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoStashMissions = Core.PlayerSettings.ToggleAutoStashMissions(SteamID);
            ctx.Reply($"AutoStash for missions is {(autoStashMissions ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "conveyor", shortHand: "co", usage: ".l co", description: "Toggles the ability of sender/receiver's to move items around.")]
        public static void ToggleConveyor(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var conveyor = Core.PlayerSettings.ToggleConveyor(SteamID);
            ctx.Reply($"Conveyor is {(conveyor ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "salvage", shortHand: "sal", usage: ".l sal", description: "Toggles the ability to salvage items from a chest named 'salvage'.")]
        public static void ToggleSalvage(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var salvage = Core.PlayerSettings.ToggleSalvage(SteamID);
            ctx.Reply($"Salvage is {(salvage ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "unitspawner", shortHand: "us", usage: ".l sp", description: "Toggles the ability to fill unit stations from a chest named 'spawner'.")]
        public static void ToggleUnitSpawner(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var spawner = Core.PlayerSettings.ToggleUnitSpawner(SteamID);
            ctx.Reply($"Spawner is {(spawner ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "brazier", shortHand: "bz", usage: ".l bz", description: "Toggles the ability to fill braziers from a chest named 'brazier'.")]
        public static void ToggleBrazier(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var brazier = Core.PlayerSettings.ToggleBrazier(SteamID);
            ctx.Reply($"Brazier is {(brazier ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "silentpull", shortHand: "sp", description: "Toggles the ability to not send messages when pulling about where they came from.")]
        public static void ToggleSilentCraftPull(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var silentCraftPull = Core.PlayerSettings.ToggleSilentPull(SteamID);
            ctx.Reply($"SilentPull is {(silentCraftPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "silentstash", shortHand: "ssh", description: "Toggles the ability to not send messages when stashing items about where they go.")]
        public static void ToggleSilentStash(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var silentStash = Core.PlayerSettings.ToggleSilentStash(SteamID);
            ctx.Reply($"SilentStash is {(silentStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "settings", shortHand: "s", usage: ".l s", description: "Displays current settings.")]
        public static void DisplaySettings(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var settings = Core.PlayerSettings.GetSettings(SteamID);
            var globalSettings = Core.PlayerSettings.GetGlobalSettings();
            ctx.Reply("KindredLogistics Settings:\n" +
                      $"SortStash{(globalSettings.SortStash ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.SortStash ? "<color=green>On</color>" : "<color=red>Off</color>")}\n" +
                      $"Pull (Global) : {(globalSettings.Pull ? "<color=green>Server Enabled</color>" : "<color=red>Server Disabled</color>")}\n" +
                      $"CraftPull{(globalSettings.CraftPull ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.CraftPull ? "<color=green>On</color>" : "<color=red>Off</color>")}\n" +
                      $"DontPullLast: {(settings.DontPullLast ? "<color=green>On</color>" : "<color=red>Off</color>")}\n" +
                      $"AutoStashMissions{(globalSettings.AutoStashMissions ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.AutoStashMissions ? "<color=green>On</color>" : "<color=red>Off</color>")}\n" +
                      $"Conveyor{(globalSettings.Conveyor ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.Conveyor ? "<color=green>On</color>" : "<color=red>Off</color>")}\n" +
                      $"Salvage{(globalSettings.Salvage ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.Salvage ? "<color=green>On</color>" : "<color=red>Off</color>")}\n" +
                      $"UnitSpawner{(globalSettings.UnitSpawner ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.UnitSpawner ? "<color=green>On</color>" : "<color=red>Off</color>")}\n" +
                      $"Brazier{(globalSettings.Brazier ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.Brazier ? "<color=green>On</color>" : "<color=red>Off</color>")}" + $" | Solar: {(globalSettings.Solar ? "<color=green>Server Enabled</color>" : "<color=red>Server Disabled</color>")}\n" +
                      $"Silent (Pull: {(settings.SilentPull ? "<color=green>On</color>" : "<color=red>Off</color>")}" + $" | Stash: { (settings.SilentStash ? "<color=green>On</color>" : "<color=red>Off</color>")})"
                      );
        }  
    }

    [CommandGroup(name: "logisticsglobal", "lg")]
    public static class LogisticsGlobal
    {

        [Command(name: "sortstash", shortHand: "ss", usage: ".lg ss", description: "Toggles autostashing on double clicking sort button for player.", adminOnly: true)]
        public static void TogglePlayerAutoStash(ChatCommandContext ctx)
        {
            var autoStash = Core.PlayerSettings.ToggleSortStash();
            ctx.Reply($"Global SortStash is {(autoStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "pull", shortHand: "p", usage: ".lg p", description: "Toggles the ability to pull items from containers.", adminOnly: true)]
        public static void TogglePlayerPull(ChatCommandContext ctx)
        {
            var pull = Core.PlayerSettings.TogglePull();
            ctx.Reply($"Global Pull is {(pull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "craftpull", shortHand: "cr", usage: ".lg cr", description: "Toggles right-clicking on recipes for missing ingredients.", adminOnly: true)]
        public static void TogglePlayerAutoPull(ChatCommandContext ctx)
        {
            var autoPull = Core.PlayerSettings.ToggleCraftPull();
            ctx.Reply($"CraftPull is {(autoPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "autostashmissions", shortHand: "asm", usage: ".lg asm", description: "Toggles autostashing for servant missions.", adminOnly: true)]
        public static void ToggleServantAutoStash(ChatCommandContext ctx)
        {
            var autoStashMissions = Core.PlayerSettings.ToggleAutoStashMissions();
            ctx.Reply($"Global AutoStash for missions is {(autoStashMissions ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "conveyor", shortHand: "co", usage: ".lg co", description: "Toggles the ability of sender/receiver's to move items around.", adminOnly: true)]
        public static void ToggleConveyor(ChatCommandContext ctx)
        {
            var conveyor = Core.PlayerSettings.ToggleConveyor();
            ctx.Reply($"Global Conveyor is {(conveyor ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "salvage", shortHand: "sal", usage: ".lg sal", description: "Toggles the ability to salvage items from a chest named 'salvage'.", adminOnly: true)]
        public static void ToggleSalvage(ChatCommandContext ctx)
        {
            var salvage = Core.PlayerSettings.ToggleSalvage();
            ctx.Reply($"Global Salvage is {(salvage ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "unitspawner", shortHand: "us", usage: ".lg sp", description: "Toggles the ability to fill unit stations from a chest named 'spawner'.", adminOnly: true)]
        public static void ToggleUnitSpawner(ChatCommandContext ctx)
        {
            var spawner = Core.PlayerSettings.ToggleUnitSpawner();
            ctx.Reply($"Global Spawner is {(spawner ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "brazier", shortHand: "bz", usage: ".lg bz", description: "Toggles the ability to fill braziers from a chest named 'brazier'.", adminOnly: true)]
        public static void ToggleBrazier(ChatCommandContext ctx)
        {
            var brazier = Core.PlayerSettings.ToggleBrazier();
            ctx.Reply($"Global Brazier is {(brazier ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "solar", shortHand:"sol", usage: ".lg sol", description: "Toggles the ability allow solar/proximity controlled braziers.", adminOnly: true)]
        public static void ToggleSolar(ChatCommandContext ctx)
        {
            var solar = Core.PlayerSettings.ToggleSolar();
            ctx.Reply($"Global Solar is {(solar ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "settings", shortHand: "s", usage: ".lg s", description: "Displays current settings.", adminOnly: true)]
        public static void DisplaySettings(ChatCommandContext ctx)
        {
            var settings = Core.PlayerSettings.GetGlobalSettings();
            ctx.Reply("KindredLogistics Global settings:\n" +
                      $"SortStash: {(settings.SortStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Pull: {(settings.Pull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"CraftPull: {(settings.CraftPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"AutoStashMissions: {(settings.AutoStashMissions ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Conveyor: {(settings.Conveyor ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Salvage: {(settings.Salvage ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"UnitSpawner: {(settings.UnitSpawner ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Brazier: {(settings.Brazier ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Solar: {(settings.Solar ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}"
                      );
        }
    }

    public static class AdditionalCommands
    {
        [Command(name: "stash", description: "Stashes all items in your inventory.")]
        public static void StashInventory(ChatCommandContext ctx)
        {
            Core.Stash.StashCharacterInventory(ctx.Event.SenderCharacterEntity);
        }

        [Command(name: "pull", description: "Pulls specified item from containers.")]
        public static void PullItem(ChatCommandContext ctx, FoundItem item, int quantity = 1)
        {
            PullService.PullItem(ctx.Event.SenderCharacterEntity, item.prefab, quantity);
        }

        [Command(name: "finditem", shortHand: "fi", description: "Finds the specified item in containers")]
        public static void FindItem(ChatCommandContext ctx, FoundItem item)
        {
            Core.Stash.ReportWhereItemIsLocated(ctx.Event.SenderCharacterEntity, item.prefab);
        }
    }
}
