using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Collections
{
    // PrefabList.ObjectSetting
    public interface IPrefabResource
    {
        string Name { get; }
        UnityEngine.Object LoadedObject { get; }

        AsyncOperationHandle LoadAssetAsync();
        AsyncOperationHandle<T> LoadAssetAsync<T>() where T : UnityEngine.Object;
        void UnloadAsset();

        AsyncOperationHandle<GameObject> InstantiateAsync(in float3 pos, in quaternion rot, in Transform parent);
        void ReleaseInstance(GameObject obj);

        string ToString();
    }
}
