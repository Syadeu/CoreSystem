using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Syadeu.Internal;
using Syadeu.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Mono
{
    [PreferBinarySerialization]
    public sealed class PrefabList : StaticSettingEntity<PrefabList>
    {
        [Serializable]
        public sealed class ObjectSetting : IPrefabResource
        {
            public string m_Name;
            [SerializeField] private AssetReference m_RefPrefab;
            public bool m_IsWorldUI = false;

            [NonSerialized] public bool m_IsRuntimeObject = false;
            [NonSerialized] public GameObject m_Prefab = null;

            [NonSerialized] private bool m_IsLoaded = false;
            [NonSerialized] private AsyncOperationHandle m_LoadHandle = default;
            [NonSerialized] private UnityEngine.Object m_LoadedObject = null;

            public string Name => m_Name;
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
                if (m_LoadHandle.IsValid())
                {
                    return m_LoadHandle;
                }
                if (m_LoadedObject != null)
                {
                    CoreSystem.Logger.LogError(Channel.Data, "already loaded");
                    return default(AsyncOperationHandle);
                }
                if (!m_RefPrefab.RuntimeKeyIsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Data, $"{m_Name} is not valid.");
                    return default(AsyncOperationHandle);
                }

                var handle = m_RefPrefab.LoadAssetAsync<UnityEngine.Object>();
                handle.Completed += Handle_Completed;

                m_IsLoaded = true;
                m_LoadHandle = handle;
                return handle;
            }
            private void Handle_Completed(AsyncOperationHandle<UnityEngine.Object> obj)
            {
                m_LoadedObject = obj.Result;
            }
            public AsyncOperationHandle<T> LoadAssetAsync<T>() where T : UnityEngine.Object
            {
                if (m_LoadHandle.IsValid())
                {
                    return m_LoadHandle.Convert<T>();
                }
                if (m_LoadedObject != null)
                {
                    CoreSystem.Logger.LogError(Channel.Data, "already loaded");
                    return default(AsyncOperationHandle<T>);
                }
                if (!m_RefPrefab.RuntimeKeyIsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Data, $"{m_Name} is not valid.");
                    return default(AsyncOperationHandle<T>);
                }
#if UNITY_EDITOR
                if (!TypeHelper.TypeOf<T>.Type.IsAssignableFrom(m_RefPrefab.editorAsset.GetType()))
                {
                    CoreSystem.Logger.LogError(Channel.Data, 
                        $"Trying to load with wrong casting type at {m_Name}. " +
                        $"Expected {m_RefPrefab.editorAsset.GetType()} but trying {TypeHelper.TypeOf<T>.Type}.");
                    return default(AsyncOperationHandle<T>);
                }
#endif

                AsyncOperationHandle<T> handle;
                try
                {
                    handle = m_RefPrefab.LoadAssetAsync<T>();
                }
                catch (InvalidKeyException)
                {
                    CoreSystem.Logger.LogError(Channel.Data,
                        $"Prefab({m_RefPrefab.AssetGUID}) is not valid. Maybe didn\'t build?");
                    return default(AsyncOperationHandle<T>);
                }
                catch (Exception)
                {
                    throw;
                }

                handle.Completed += this.AsynHandleOnCompleted;

                m_IsLoaded = true;
                m_LoadHandle = handle;
                return handle;
            }
            private void AsynHandleOnCompleted<T>(AsyncOperationHandle<T> obj) where T : UnityEngine.Object
            {
                m_LoadedObject = obj.Result;

                m_LoadHandle = default;
            }

            public void UnloadAsset()
            {
                if (!m_RefPrefab.RuntimeKeyIsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Data, $"{m_Name} is not valid.");
                    return;
                }

                m_LoadedObject = null;
                m_RefPrefab.ReleaseAsset();
            }

            #endregion

            public AsyncOperationHandle<GameObject> InstantiateAsync(in float3 pos, in quaternion rot, in Transform parent)
            {
                if (!m_RefPrefab.RuntimeKeyIsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Data, $"{m_Name} is not valid.");
                    return default(AsyncOperationHandle<GameObject>);
                }

                return m_RefPrefab.InstantiateAsync(pos, rot, parent);
            }

            public void ReleaseInstance(GameObject obj) => m_RefPrefab.ReleaseInstance(obj);

            public override string ToString() => m_Name;
        }

        [SerializeField] private List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        public List<ObjectSetting> ObjectSettings => m_ObjectSettings;
    }
}
