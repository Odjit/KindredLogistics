using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using KindredLogistics.Commands.Converters;
using KindredLogistics.Services;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

namespace KindredLogistics;

internal static class Core
{
	public static World Server { get; } = GetWorld("Server") ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");

    // V Rising systems
	public static EntityManager EntityManager { get; } = Server.EntityManager;
    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
    public static ServerGameSettingsSystem ServerGameSettingsSystem { get; internal set; }
    public static ServerScriptMapper ServerScriptMapper { get; internal set; }
    public static DebugEventsSystem DebugEventsSystem { get; internal set; }
    public static double ServerTime => ServerGameManager.ServerTime;
    public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();

    // BepInEx services
    public static ManualLogSource Log => Plugin.LogInstance;

    // KindredLogistics services
    public static ConveyorService ConveyorService { get; internal set; }

    public static LocalizationService Localization { get; } = new();
    public static PlayerSettingsService PlayerSettings { get; } = new();
    public static RefinementStationsService RefinementStations { get; internal set; }
    public static RegionService RegionService { get; internal set; }
    public static StashService Stash { get; } = new();
    public static TerritoryService TerritoryService { get; internal set; }

    public const int MAX_REPLY_LENGTH = 509;

    static bool hasInitialized;

    static MonoBehaviour monoBehaviour;

    public static void LogException(System.Exception e, [CallerMemberName] string caller = null)
    {
        Core.Log.LogError($"Failure in {caller}\nMessage: {e.Message} Inner:{e.InnerException?.Message}\n\nStack: {e.StackTrace}\nInner Stack: {e.InnerException?.StackTrace}");
    }

    public static void Initialize()
    {
        if (hasInitialized) return;

        PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        ServerGameSettingsSystem = Server.GetExistingSystemManaged<ServerGameSettingsSystem>();
        DebugEventsSystem = Server.GetExistingSystemManaged<DebugEventsSystem>();
        ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();

        // Initialize utility services
        RefinementStations = new();
        RegionService = new();
        TerritoryService = new();

        // Now start services that actually do stuff
        ConveyorService = new();

        FoundItemConverter.LoadItemNames();

        Core.Log.LogInfo("KindredLogistics initialized");

        hasInitialized = true;
    }

    static World GetWorld(string name)
	{
		foreach (var world in World.s_AllWorlds)
		{
			if (world.Name == name)
			{
				return world;
			}
		}

		return null;
	}

    public static void StartCoroutine(IEnumerator routine)
    {
        if (monoBehaviour == null)
        {
            var go = new GameObject("KindredLogistics");
            monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
            Object.DontDestroyOnLoad(go);
        }

        monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
}
