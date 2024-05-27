using KindredLogistics;
using KindredLogistics.Commands.Converters;
using KindredLogistics.Services;
using VampireCommandFramework;

namespace Logistics.Commands
{
    [CommandGroup(name: "logistics", "l")]
    public static class LogisticsCommands
    {
        [Command(name: "遠程存物", shortHand: "ss", usage: ".l ss", description: "雙擊排序按鈕存入倉庫")]
        public static void TogglePlayerAutoStash(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoStash = Core.PlayerSettings.ToggleSortStash(SteamID);
            ctx.Reply($"遠程存物 {(autoStash ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}.");
        }
       
        [Command(name: "遠程取物", shortHand: "cr", usage: ".l cr", description: "右鍵點擊製作道具以查找傳送缺少的材料")]
        public static void TogglePlayerAutoPull(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoPull = Core.PlayerSettings.ToggleCraftPull(SteamID);
            ctx.Reply($"遠程取物 {(autoPull ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}.");
        }

        [Command(name: "dontpulllast", shortHand: "dpl", usage: ".l dpl", description: "Toggles the ability to not pull the last item from a container for Logistics commands.", adminOnly: true)]
        public static void ToggleDontPullLast(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var dontPullLast = Core.PlayerSettings.ToggleDontPullLast(SteamID);
            ctx.Reply($"DontPullLast is {(dontPullLast ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}.");
        }

        [Command(name: "autostashmissions", shortHand: "asm", usage: ".l asm", description: "Toggles autostashing for servant missions.", adminOnly: true)]
        public static void ToggleServantAutoStash(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var autoStashMissions = Core.PlayerSettings.ToggleAutoStashMissions(SteamID);
            ctx.Reply($"AutoStash for missions is {(autoStashMissions ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}.");
        }

        [Command(name: "conveyor", shortHand: "co", usage: ".l co", description: "Toggles the ability of sender/receiver's to move items around.", adminOnly: true)]
        public static void ToggleConveyor(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var conveyor = Core.PlayerSettings.ToggleConveyor(SteamID);
            ctx.Reply($"Conveyor is {(conveyor ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}.");
        }

        [Command(name: "settings", shortHand: "s", usage: ".l s", description: "Displays current settings.")]
        public static void DisplaySettings(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            var settings = Core.PlayerSettings.GetSettings(SteamID);
            var globalSettings = Core.PlayerSettings.GetGlobalSettings();
            ctx.Reply("KindredLogistics settings:\n" +
                      $"SortStash{(globalSettings.SortStash ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.SortStash ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}\n" +
                      $"Pull (Global) : {(globalSettings.Pull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"CraftPull{(globalSettings.CraftPull ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.CraftPull ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}\n" +
                      $"DontPullLast: {(settings.DontPullLast ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"AutoStashMissions{(globalSettings.AutoStashMissions ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.AutoStashMissions ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}\n" +
                      $"Conveyor{(globalSettings.Conveyor ? "" : "(<color=red>Server Disabled</color>)")}: {(settings.Conveyor ? "<color=green>已啟動</color>" : "<color=red>已關閉</color>")}"
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

        [Command(name: "settings", shortHand: "s", usage: ".lg s", description: "Displays current settings.", adminOnly: true)]
        public static void DisplaySettings(ChatCommandContext ctx)
        {
            var settings = Core.PlayerSettings.GetGlobalSettings();
            ctx.Reply("KindredLogistics Global settings:\n" +
                      $"SortStash: {(settings.SortStash ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Pull: {(settings.Pull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"CraftPull: {(settings.CraftPull ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"AutoStashMissions: {(settings.AutoStashMissions ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}\n" +
                      $"Conveyor: {(settings.Conveyor ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}"
                      );
        }
    }

    public static class AdditionalCommands
    {
        [Command(name: "stash", shortHand: "存入倉庫", description: "你背包的道具以幫您傳送回該所屬的倉庫了.")]
        public static void StashInventory(ChatCommandContext ctx)
        {
            Core.Stash.StashCharacterInventory(ctx.Event.SenderCharacterEntity);
        }

        [Command(name: "pull", description: "從倉庫中取出指定的道具.")]
        public static void PullItem(ChatCommandContext ctx, FoundItem item, int quantity = 1)
        {
            PullService.PullItem(ctx.Event.SenderCharacterEntity, item.prefab, quantity);
        }

        [Command(name: "finditem", shortHand: "fi", description: "尋找倉庫中指定的道具")]
        public static void FindItem(ChatCommandContext ctx, FoundItem item)
        {
            Core.Stash.ReportWhereItemIsLocated(ctx.Event.SenderCharacterEntity, item.prefab);
        }
    }
}
