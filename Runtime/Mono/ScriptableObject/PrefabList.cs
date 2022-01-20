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

            [NonSerialized] private int m_InstantateCount = 0;
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

            [Obsolete]
            public UnityEngine.Object LoadAsset()
            {
                if (m_LoadedObject == null)
                {
                    var temp = m_RefPrefab.LoadAsset<UnityEngine.Object>();
                    m_LoadedObject = temp.Result;
                }

                return m_LoadedObject;
            }
            public AsyncOperationHandle LoadAssetAsync()
            {
                if (m_LoadHandle.IsValid())
                {
                    return m_LoadHandle;
                }
                if (m_LoadedObject != null)
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
                handle.Completed += Handle_Completed;

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
                else if (m_LoadedObject != null)
                {
                    CoreSystem.Logger.LogError(Channel.Data, "already loaded");
                    return default(AsyncOperationHandle<T>);
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

                handle.Completed += this.AsynHandleOnCompleted;

                m_LoadHandle = handle;
                return handle;
            }
            private void AsynHandleOnCompleted<T>(AsyncOperationHandle<T> obj) where T : UnityEngine.Object
            {
                m_LoadedObject = obj.Result;

                m_LoadHandle = default;

                CoreSystem.Logger.Log(Channel.Data,
                    $"Loaded asset {m_Name}");
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

                m_LoadedObject = null;
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
                    m_LoadedObject = null;
                    UnloadAsset();
                }
            }

            public override string ToString() => m_Name;
        }

        [SerializeField] private List<ObjectSetting> m_ObjectSettings = new List<ObjectSetting>();

        public List<ObjectSetting> ObjectSettings => m_ObjectSettings;
    }
}
