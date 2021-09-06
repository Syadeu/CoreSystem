using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Mono
{
    [PreferBinarySerialization]
    public sealed class PrefabList : StaticSettingEntity<PrefabList>
    {
        [Serializable]
        public sealed class ObjectSetting
        {
            public string m_Name;
            [SerializeField] private AssetReference m_RefPrefab;
            public bool m_IsWorldUI = false;

            [NonSerialized] public bool m_IsRuntimeObject = false;
            [NonSerialized] public GameObject m_Prefab = null;

            [NonSerialized] private UnityEngine.Object m_LoadedObject = null;

            public UnityEngine.Object LoadedObject => m_LoadedObject;

            public ObjectSetting(string name, AssetReference refPrefab, bool isWorldUI)
            {
                m_Name = name;
                m_RefPrefab = refPrefab;
                m_IsWorldUI = isWorldUI;
            }

            #region Resource Control
            public AsyncOperationHandle LoadAssetAsync()
            {
                if (m_LoadedObject != null)
                {
                    CoreSystem.Logger.LogError(Channel.Data, "already loaded");
                    return default(AsyncOperationHandle);
                }

                var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(m_RefPrefab);
                handle.Completed += Handle_Completed;
                return handle;
            }
            private void Handle_Completed(AsyncOperationHandle<UnityEngine.Object> obj)
            {
                m_LoadedObject = obj.Result;
            }
            public AsyncOperationHandle<T> LoadAssetAsync<T>() where T : UnityEngine.Object
            {
                if (m_LoadedObject != null)
                {
                    CoreSystem.Logger.LogError(Channel.Data, "already loaded");
                    return default(AsyncOperationHandle<T>);
                }
                
                var handle = Addressables.LoadAssetAsync<T>(m_RefPrefab);
                handle.Completed += this.Handle_Completed1;
                return handle;
            }
            private void Handle_Completed1<T>(AsyncOperationHandle<T> obj) where T : UnityEngine.Object
            {
                m_LoadedObject = obj.Result;
            }

            public void UnloadAsset()
            {
                m_LoadedObject = null;
                m_RefPrefab.ReleaseAsset();
            }

            #endregion

            public AsyncOperationHandle<GameObject> InstantiateAsync(float3 pos, quaternion rot, Transform parent) => m_RefPrefab.InstantiateAsync(pos, rot, parent);

            public void ReleaseInstance(GameObject obj) => m_RefPrefab.ReleaseInstance(obj);

            public override string ToString() => m_Name;
        }

        [SerializeField] private List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        public List<ObjectSetting> ObjectSettings => m_ObjectSettings;
    }
}
