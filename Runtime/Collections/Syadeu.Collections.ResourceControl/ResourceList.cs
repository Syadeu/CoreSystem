// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

using AddressableReference = UnityEngine.AddressableAssets.AssetReference;

namespace Syadeu.Collections.ResourceControl
{
    public sealed class ResourceList : ScriptableObject
    {
        [SerializeField] private List<AddressableAsset> m_AssetList = new List<AddressableAsset>();

        public AddressableAsset this[int index]
        {
            get => m_AssetList[index];
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor only
        /// </summary>
        /// <param name="name"></param>
        /// <param name="asset"></param>
        public void AddAsset(string name, AddressableReference asset)
        {
            AddressableAsset temp = new AddressableAsset(name, asset.AssetGUID);
            m_AssetList.Add(temp);
        }
        /// <summary>
        /// Editor only
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        public void AddAsset(string name, UnityEngine.Object obj)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            var guid = UnityEditor.AssetDatabase.GUIDFromAssetPath(path);
            AddressableAsset temp = new AddressableAsset(name, guid.ToString());

            m_AssetList.Add(temp);
        }
#endif
    }
    public sealed class ResourceHashMap : StaticScriptableObject<ResourceHashMap>
    {
        [SerializeField] private List<ResourceList> m_ResourceLists = new List<ResourceList>();

        public ResourceList this[int index]
        {
            get
            {
                return m_ResourceLists[index];
            }
        }
        public AddressableAsset this[int2 index]
        {
            get
            {
                return m_ResourceLists[index.x][index.y];
            }
        }
    }

    [Serializable]
    public struct AssetReference : IValidation, IEquatable<AssetReference>
    {
        [SerializeField] private int2 m_Index;
        [SerializeField] private FixedString128Bytes m_SubAssetName;

        private bool m_IsCreated;

        public bool IsCreated => m_IsCreated;
        public bool IsSubAsset => !m_SubAssetName.IsEmpty;

        public AssetReference(int2 index)
        {
            m_Index = index;
            m_SubAssetName = default(FixedString128Bytes);

            m_IsCreated = true;
        }
        public AssetReference(int2 index, FixedString128Bytes subAssetName)
        {
            m_Index = index;
            m_SubAssetName = subAssetName;

            m_IsCreated = true;
        }

        public bool IsValid()
        {
            if (!m_IsCreated) return false;

            return true;
        }
        public bool Equals(AssetReference other) => m_Index.Equals(other.m_Index);

        public AddressableAsset GetAddressableAsset()
        {
            if (!IsValid()) return null;

            return ResourceHashMap.Instance[m_Index];
        }

        public static implicit operator AddressableAsset(AssetReference t) => t.GetAddressableAsset();
    }
    [Serializable]
    public sealed class AddressableAsset : AddressableReference, IPromiseProvider<UnityEngine.Object>
    {
        [SerializeField] private string m_DisplayName;
        private Action<UnityEngine.Object> m_OnComplete;
        private int m_InstanceCount = 0;

        public string DisplayName { get => m_DisplayName; set => m_DisplayName = value; }

        public event Action<UnityEngine.Object> OnComplete
        {
            add
            {
                if (IsDone)
                {
                    value?.Invoke(Asset);
                    return;
                }

                m_OnComplete += value;
            }
            remove
            {
                m_OnComplete -= value;
            }
        }

        public AddressableAsset() : base() { }
#if UNITY_EDITOR
        public AddressableAsset(string name, UnityEditor.GUID guid) : this(name, guid.ToString()) { }
#endif
        public AddressableAsset(string name, string guid) : base(guid)
        {
            m_DisplayName = name;
        }

        public override AsyncOperationHandle<TObject> LoadAssetAsync<TObject>()
        {
            var handle = base.LoadAssetAsync<TObject>();
            handle.CompletedTypeless += Handle_CompletedTypeless;

            return handle;
        }
        public override AsyncOperationHandle<GameObject> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            var handle = base.InstantiateAsync(parent, instantiateInWorldSpace);
            m_InstanceCount++;

            return handle;
        }
        public override AsyncOperationHandle<GameObject> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var handle = base.InstantiateAsync(position, rotation, parent);
            m_InstanceCount++;

            return handle;
        }
        public override void ReleaseInstance(GameObject obj)
        {
            base.ReleaseInstance(obj);
            m_InstanceCount--;
        }
        public override void ReleaseAsset()
        {
            base.ReleaseAsset();
        }
        
        void IPromiseProvider<UnityEngine.Object>.OnComplete(Action<UnityEngine.Object> obj)
        {
            if (IsDone)
            {
                obj?.Invoke(Asset);

                return;
            }

            m_OnComplete += obj;
        }
        private void Handle_CompletedTypeless(AsyncOperationHandle obj)
        {
            m_OnComplete?.Invoke(obj.Result as UnityEngine.Object);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}