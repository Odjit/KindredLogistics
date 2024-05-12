using KindredLogistics;
using KindredLogistics.Commands.Converters;
using KindredLogistics.Services;
using ProjectM.Network;
using VampireCommandFramework;

namespace Logistics.Commands
{
    [CommandGroup(name: "logistics", "l")]
    public static class LogisticsCommands
    {
        [Command(name: "sortStash", shortHand: "ss", usage: ".l ss", description: "Toggles autostashing on double clicking sort button for player.")]
        public static void TogglePlayerAutoStash(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoStash = Core.PlayerSettings.ToggleSortStash(SteamID);
            ctx.Reply($"SortStash is {(autoStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }
       
        [Command(name: "craftPull", shortHand: "cr", usage: ".l cr", description: "Toggles right-clicking on recipes for missing ingredients.")]
        public static void TogglePlayerAutoPull(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoPull = Core.PlayerSettings.ToggleCraftPull(SteamID);
            ctx.Reply($"CraftPull is {(autoPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "dontPullLast", shortHand: "dpl", usage: ".l dpl", description: "Toggles the ability to not pull the last item from a container for Logistics commands.")]
        public static void ToggleDontPullLast(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var dontPullLast = Core.PlayerSettings.ToggleDontPullLast(SteamID);
            ctx.Reply($"DontPullLast is {(dontPullLast ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "autoStashMissions", shortHand: "asm", usage: ".l asm", description: "Toggles autostashing for servant missions.")]
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

        [Command(name: "settings", shortHand: "s", usage: ".l s", description: "Displays current settings.")]
        public static void DisplaySettings(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var settings = Core.PlayerSettings.GetSettings(SteamID);
            var globalSettings = Core.PlayerSettings.GetGlobalSettings();
            ctx.Reply("KindredLogistics settings:\n" +
                      $"SortStash{(globalSettings.SortStash ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.SortStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"CraftPull{(globalSettings.CraftPull ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.CraftPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"DontPullLast: {(settings.DontPullLast ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"AutoStashMissions{(globalSettings.AutoStashMissions ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.AutoStashMissions ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Conveyor{(globalSettings.Conveyor ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.Conveyor ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}"
                      );
        }  
    }

    [CommandGroup(name: "logisticsglobal", "lg")]
    public static class LogisticsGlobal
    {

        [Command(name: "sortStash", shortHand: "ss", usage: ".lg ss", description: "Toggles autostashing on double clicking sort button for player.", adminOnly: true)]
        public static void TogglePlayerAutoStash(ChatCommandContext ctx)
        {
            var autoStash = Core.PlayerSettings.ToggleSortStash();
            ctx.Reply($"Global SortStash is {(autoStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "craftPull", shortHand: "cr", usage: ".lg cr", description: "Toggles right-clicking on recipes for missing ingredients.", adminOnly: true)]
        public static void TogglePlayerAutoPull(ChatCommandContext ctx)
        {
            var autoPull = Core.PlayerSettings.ToggleCraftPull();
            ctx.Reply($"CraftPull is {(autoPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }
        [Command(name: "autoStashMissions", shortHand: "asm", usage: ".lg asm", description: "Toggles autostashing for servant missions.", adminOnly: true)]
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

        [Command(name: "settings", shortHand: "s", usage: ".lg s", description: "Displays current settings.", adminOnly: true)]
        public static void DisplaySettings(ChatCommandContext ctx)
        {
            var settings = Core.PlayerSettings.GetGlobalSettings();
            ctx.Reply("KindredLogistics Global settings:\n" +
                      $"SortStash: {(settings.SortStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"CraftPull: {(settings.CraftPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"AutoStashMissions: {(settings.AutoStashMissions ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Conveyor: {(settings.Conveyor ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}"
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
        public static void PullItem(ChatCommandContext ctx, FoundItem item, int quantity=1)
        {
            var amountRemaining = PullService.PullItem(ctx.Event.SenderCharacterEntity, item.prefab, quantity);
            if(amountRemaining <= 0)
                ctx.Reply($"Pulled {quantity}x {item.prefab.PrefabName()} from containers.");
            else
                ctx.Reply($"Pulled {quantity - amountRemaining}x {item.prefab.PrefabName()} from containers. Couldn't find {amountRemaining}x");
        }

        [Command(name: "finditem", shortHand: "fi", description: "Finds the specified item in containers")]
        public static void FindItem(ChatCommandContext ctx, FoundItem item)
        {
            Core.Stash.ReportWhereItemIsLocated(ctx.Event.SenderCharacterEntity, item.prefab);
        }
    }
}
