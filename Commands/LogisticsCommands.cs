using KindredLogistics;
using VampireCommandFramework;

namespace Logistics.Commands
{
    [CommandGroup(name: "logistics", "l")]
    public static class LogisticsCommands
    {
        [Command(name: "autoStashPlayer", shortHand: "as", adminOnly: false, usage: ".l as", description: "Toggles autostashing on double clicking sort button for player.")]
        public static void TogglePlayerAutoStash(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoStash = Core.PlayerSettings.ToggleSortStash(SteamID);
            ctx.Reply($"AutoStash is {(autoStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }
       
        [Command(name: "autoPullPlayer", shortHand: "ap", adminOnly: false, usage: ".l ap", description: "Toggles right-clicking on recipes for missing ingredients.")]
        public static void TogglePlayerAutoPull(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoPull = Core.PlayerSettings.ToggleCraftPull(SteamID);
            ctx.Reply($"AutoPull is {(autoPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }
        [Command(name: "autoStashMissions", shortHand: "am", adminOnly: false, usage: ".l am", description: "Toggles autostashing for servant missions.")]
        public static void ToggleServantAutoStash(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoStashMissions = Core.PlayerSettings.ToggleAutoStashMissions(SteamID);
            ctx.Reply($"AutoStash for missions is {(autoStashMissions ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command("settings", "s", usage: ".l s", description: "Displays current settings.")]
        public static void DisplaySettings(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var settings = Core.PlayerSettings.GetSettings(SteamID);
            ctx.Reply("KindredLogistics settings:\n" +
                      $"AutoStash: {(settings.SortStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"AutoPull: {(settings.CraftPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"AutoStashMissions: {(settings.AutoStashMissions ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}");
        }
       
    }
}
