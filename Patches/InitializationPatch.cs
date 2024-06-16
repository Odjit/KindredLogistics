using HarmonyLib;
using Unity.Scenes;

namespace KindredLogistics.Patches;

[HarmonyPatch(typeof(SceneSystem), nameof(SceneSystem.ShutdownStreamingSupport))]
public static class InitializationPatch
{
    [HarmonyPostfix]
    public static void OneShot_AfterLoad_InitializationPatch()
    {
        Core.Initialize();
        Plugin.Harmony.Unpatch(typeof(SceneSystem).GetMethod("ShutdownStreamingSupport"), typeof(InitializationPatch).GetMethod("OneShot_AfterLoad_InitializationPatch"));
    }
}
