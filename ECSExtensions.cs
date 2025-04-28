using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using System;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace KindredLogistics;

public static class ECSExtensions
{
    static EntityManager EntityManager { get; } = Core.Server.EntityManager;
   
    public static unsafe void Write<T>(this Entity entity, T componentData) where T : struct
    {
        // Get the ComponentType for T
        var ct = new ComponentType(Il2CppType.Of<T>());
        
        // Marshal the component data to a byte array
        byte[] byteArray = StructureToByteArray(componentData);

        // Get the size of T
        int size = Marshal.SizeOf<T>();

        // Create a pointer to the byte array
        fixed (byte* p = byteArray)
        {
            // Set the component data
            EntityManager.SetComponentDataRaw(entity, ct.TypeIndex, p, size);
        }
    }

    // Helper function to marshal a struct to a byte array
    static byte[] StructureToByteArray<T>(T structure) where T : struct
    {
        int size = Marshal.SizeOf(structure);
        byte[] byteArray = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(structure, ptr, true);
        Marshal.Copy(ptr, byteArray, 0, size);
        Marshal.FreeHGlobal(ptr);

        return byteArray;
    }

    public static unsafe T Read<T>(this Entity entity) where T : struct
    {
        // Get the ComponentType for T
        var ct = new ComponentType(Il2CppType.Of<T>());

        // Get a pointer to the raw component data
        void* rawPointer = EntityManager.GetComponentDataRawRO(entity, ct.TypeIndex);

        // Marshal the raw data to a T struct
        T componentData = Marshal.PtrToStructure<T>(new IntPtr(rawPointer));

        return componentData;
    }

    public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
    {
        return Core.Server.EntityManager.GetBuffer<T>(entity);
    }

    public static bool Has<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        return EntityManager.HasComponent(entity, ct);
    }

    public static string LookupName(this PrefabGUID prefabGuid)
    {
        var prefabCollectionSystem = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        return (prefabCollectionSystem._PrefabLookupMap.TryGetName(prefabGuid, out var name)
            ? name + " " + prefabGuid : "GUID Not Found").ToString();
    }

    public static string PrefabName(this PrefabGUID prefabGuid)
    {
        var name = Core.Localization.GetPrefabName(prefabGuid);
        return string.IsNullOrEmpty(name) ? prefabGuid.LookupName() : name;
    }

    public static string EntityName(this Entity entity)
    {
        var nameable = entity.Read<NameableInteractable>();
        var name = nameable.Name.ToString();

        if(string.IsNullOrEmpty(name) && entity.Has<PrefabGUID>())
            name = entity.Read<PrefabGUID>().PrefabName();

        if (string.IsNullOrEmpty(name))
            name = entity.ToString();

        return name;
    }

    public static void Add<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        EntityManager.AddComponent(entity, ct);
    }

    public static void Remove<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        EntityManager.RemoveComponent(entity, ct);
    }
}

