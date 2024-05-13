using BepInEx.Logging;
using KindredLogistics.Commands.Converters;
using KindredLogistics.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Unity.Entities;

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

    private static World GetWorld(string name)
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
}
