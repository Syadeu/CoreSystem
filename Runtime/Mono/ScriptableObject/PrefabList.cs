// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

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
using Unity.Collections;
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
            [SerializeField] public AssetReference m_RefPrefab;
            public bool m_IsWorldUI = false;

            [NonSerialized] public bool m_IsRuntimeObject = false;
            [NonSerialized] public GameObject m_Prefab = null;

            [NonSerialized] private int m_InstantateCount = 0;
            //[NonSerialized] private UnityEngine.Object m_LoadedObject = null;

            [NonSerialized] private Dictionary<FixedString128Bytes, AssetReference> m_SubAssets;

            public string Name => m_Name;
            //public UnityEngine.Object LoadedObject => m_RefPrefab.Asset;

            public ObjectSetting(string name, AssetReference refPrefab, bool isWorldUI)
            {
                m_Name = name;
                m_RefPrefab = refPrefab;
                m_IsWorldUI = isWorldUI;
            }

            public UnityEngine.Object GetLoadedObject(FixedString128Bytes name)
            {
                if (!name.IsEmpty)
                {
                    if (m_SubAssets == null) m_SubAssets = new Dictionary<FixedString128Bytes, AssetReference>();

                    if (!m_SubAssets.TryGetValue(name, out var assetRef))
                    {
                        assetRef = new AssetReference(m_RefPrefab.AssetGUID);
                        assetRef.SubObjectName = name.ToString();

                        m_SubAssets.Add(name, assetRef);
                    }
                    return assetRef.Asset;
                }

                return m_RefPrefab.Asset;
            }

            #region Resource Control

            [Obsolete]
            public UnityEngine.Object LoadAsset(FixedString128Bytes name)
            {
                if (!name.IsEmpty)
                {
                    if (m_SubAssets == null) m_SubAssets = new Dictionary<FixedString128Bytes, AssetReference>();

                    if (!m_SubAssets.TryGetValue(name, out var assetRef))
                    {
                        assetRef = new AssetReference(m_RefPrefab.AssetGUID);
                        assetRef.SubObjectName = name.ToString();

                        m_SubAssets.Add(name, assetRef);
                    }

                    if (assetRef.Asset == null)
                    {
                        assetRef.LoadAsset<UnityEngine.Object>();
                    }

                    return assetRef.Asset;
                }

                if (m_RefPrefab.Asset == null)
                {
                    m_RefPrefab.LoadAsset<UnityEngine.Object>();
                }

                return m_RefPrefab.Asset;
            }
            public AsyncOperationHandle LoadAssetAsync(FixedString128Bytes name)
            {
                if (!name.IsEmpty)
                {
                    if (m_SubAssets == null) m_SubAssets = new Dictionary<FixedString128Bytes, AssetReference>();

                    if (!m_SubAssets.TryGetValue(name, out var assetRef))
                    {
                        assetRef = new AssetReference(m_RefPrefab.AssetGUID);
                        assetRef.SubObjectName = name.ToString();

                        m_SubAssets.Add(name, assetRef);
                    }

                    if (assetRef.OperationHandle.IsValid())
                    {
                        return assetRef.OperationHandle;
                    }

                    return assetRef.LoadAssetAsync<UnityEngine.Object>();

                    //return Addressables.LoadAssetAsync<UnityEngine.Object>(m_RefPrefab.AssetGUID + $"[{name}]");
                }

                if (m_RefPrefab.OperationHandle.IsValid())
                {
                    return m_RefPrefab.OperationHandle;
                }
                if (m_RefPrefab.Asset != null)
                {
                    CoreSystem.Logger.LogError(Channel.Data, $"{m_Name} already loaded. This is not allowed.");

                    return m_RefPrefab.OperationHandle;
                }
                if (!m_RefPrefab.RuntimeKeyIsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Data, $"{m_Name} is not valid.");
                    return default(AsyncOperationHandle);
                }

                var handle = m_RefPrefab.LoadAssetAsync<UnityEngine.Object>();
                //handle.Completed += Handle_Completed;

                return handle;
            }
            public AsyncOperationHandle<T> LoadAssetAsync<T>(FixedString128Bytes name) where T : UnityEngine.Object
            {
                if (!name.IsEmpty)
                {
                    if (m_SubAssets == null) m_SubAssets = new Dictionary<FixedString128Bytes, AssetReference>();

                    if (!m_SubAssets.TryGetValue(name, out var assetRef))
                    {
                        assetRef = new AssetReference(m_RefPrefab.AssetGUID);
                        assetRef.SubObjectName = name.ToString();

                        m_SubAssets.Add(name, assetRef);
                    }

                    if (assetRef.OperationHandle.IsValid())
                    {
                        return assetRef.OperationHandle.Convert<T>();
                    }

                    return assetRef.LoadAssetAsync<T>();

                    //return Addressables.LoadAssetAsync<T>(m_RefPrefab.AssetGUID + $"[{name}]");
                }

                if (m_RefPrefab.Asset != null)
                {
                    CoreSystem.Logger.LogError(Channel.Data, "already loaded");
                    return default(AsyncOperationHandle<T>);
                }
                else if (m_RefPrefab.OperationHandle.IsValid())
                {
                    return m_RefPrefab.OperationHandle.Convert<T>();
                }
                else if (!m_RefPrefab.RuntimeKeyIsValid())
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

                return handle;
            }

            public void UnloadAsset()
            {
                if (!m_RefPrefab.RuntimeKeyIsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Data, $"{m_Name} is not valid.");
                    return;
                }
                else if (m_InstantateCount > 0)
                {
                    CoreSystem.Logger.LogError(Channel.Data,
                        $"{m_Name} is not fully released but trying to unload.");
                }

                //m_LoadedObject = null;
                m_RefPrefab.ReleaseAsset();

                CoreSystem.Logger.Log(Channel.Data,
                    $"Unload asset {m_Name}");
            }

            #endregion

            public AsyncOperationHandle<GameObject> InstantiateAsync(in float3 pos, in quaternion rot, in Transform parent)
            {
                if (!m_RefPrefab.RuntimeKeyIsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Data, $"{m_Name} is not valid.");
                    return default(AsyncOperationHandle<GameObject>);
                }

                m_InstantateCount++;
                return m_RefPrefab.InstantiateAsync(pos, rot, parent);
            }

            public void ReleaseInstance(GameObject obj)
            {
                m_RefPrefab.ReleaseInstance(obj);
                m_InstantateCount--;

                if (m_InstantateCount == 0)
                {
                    //m_LoadedObject = null;
                    UnloadAsset();
                }
            }

            public override string ToString() => m_Name;
        }

        [SerializeField] private List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();
        private Dictionary<UnityEngine.Object, int> m_PrefabHashMap;

        public List<ObjectSetting> ObjectSettings => m_ObjectSettings;

        public override void OnInitialize()
        {
            ReInitialize();
        }
        public void ReInitialize()
        {
            m_PrefabHashMap = new Dictionary<UnityEngine.Object, int>();
            for (int i = 0; i < m_ObjectSettings.Count; i++)
            {
                var obj = m_ObjectSettings[i].LoadAsset(string.Empty);
                if (obj == null) continue;

                m_PrefabHashMap.Add(obj, i);
            }
        }

        public ObjectSetting GetSettingWithObject(UnityEngine.Object obj)
        {
            if (!m_PrefabHashMap.TryGetValue(obj, out int index))
            {
                return null;
            }

            return m_ObjectSettings[index];
        }
    }
}
