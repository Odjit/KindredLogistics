using HarmonyLib;
using Unity.Scenes;

namespace KindredLogistics.Patches;

[HarmonyPatch(typeof(SceneSectionStreamingSystem), nameof(SceneSectionStreamingSystem.ShutdownAsynchrnonousStreamingSupport))]
public static class InitializationPatch
{
    [HarmonyPostfix]
    public static void OneShot_AfterLoad_InitializationPatch()
    {
        Core.Initialize();
        Plugin.Harmony.Unpatch(typeof(SceneSectionStreamingSystem).GetMethod("ShutdownAsynchrnonousStreamingSupport"), typeof(InitializationPatch).GetMethod("OneShot_AfterLoad_InitializationPatch"));
    }
}
