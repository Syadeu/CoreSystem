// Copyright 2021 Ikina Games
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

#if UNITY_2019_1_OR_NEWER && UNITY_ADDRESSABLES
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE
#if UNITY_2019 && !UNITY_2020_1_OR_NEWER
#define UNITYENGINE_OLD
#else
#if UNITY_MATHEMATICS
#endif
#endif

using Syadeu.Collections.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using AddressableReference = UnityEngine.AddressableAssets.AssetReference;

namespace Syadeu.Collections.ResourceControl
{
    public sealed class ResourceList : ScriptableObject
    {
        [SerializeField] private GroupReference m_Group;
        [SerializeField] private List<AddressableAsset> m_AssetList = new List<AddressableAsset>();

        //[NonSerialized] private IList<AddressableReference> m_AssetReferences;

        public int Count => m_AssetList.Count;
        public AssetReference this[int index]
        {
            get => m_AssetList[index].AssetReference;
        }
        public AssetReference this[string friendlyName]
        {
            get
            {
                for (int i = 0; i < m_AssetList.Count; i++)
                {
                    if (m_AssetList[i].FriendlyName.Equals(friendlyName))
                    {
                        return m_AssetList[i].AssetReference;
                    }
                }
                return AssetReference.Empty;
            }
        }

        private void OnEnable()
        {
            //m_AssetReferences = m_AssetList.Select(t => t.AssetReference).ToArray();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor only
        /// </summary>
        public string Group { get => m_Group; set => m_Group = new GroupReference(value); }

        /// <summary>
        /// Editor only
        /// </summary>
        public void Clear()
        {
            m_AssetList.Clear();
        }
        /// <summary>
        /// Editor only
        /// </summary>
        /// <param name="name"></param>
        /// <param name="asset"></param>
        public void AddAsset(string name, string assetGuid)
        {
            AddressableAsset temp = new AddressableAsset(name, assetGuid);
            m_AssetList.Add(temp);
        }
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
        /// <summary>
        /// Editor only
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool Contains(string guid)
        {
            for (int i = 0; i < m_AssetList.Count; i++)
            {
                if (m_AssetList[i].AssetReference.AssetGUID.Equals(guid)) return true;
            }
            return false;
        }
#endif
        public AddressableAsset GetAddressableAsset(int index)
        {
            return m_AssetList[index];
        }
        public int IndexOf(string key, string subAssetName)
        {
            if (subAssetName.IsNullOrEmpty())
            {
                for (int i = 0; i < m_AssetList.Count; i++)
                {
                    var refAsset = m_AssetList[i].AssetReference;
                    if (refAsset.AssetGUID.Equals(key))
                    {
                        return i;
                    }
                }
                return -1;
            }

            for (int i = 0; i < m_AssetList.Count; i++)
            {
                var refAsset = m_AssetList[i].AssetReference;
                if (refAsset.AssetGUID.Equals(key) &&
                    refAsset.SubObjectName.Equals(subAssetName))
                {
                    return i;
                }
            }
            return -1;
        }

        //public AsyncOperationHandle<IList<UnityEngine.Object>> LoadAssetsAsync(Action<UnityEngine.Object> callback)
        //{
        //    if (m_AssetReferences.Count == 0)
        //    {
        //        IList<UnityEngine.Object> temp = Array.Empty<UnityEngine.Object>();
        //        return ResourceManager.CreateCompletedOperation(temp);
        //    }

        //    var result = Addressables.LoadAssetsAsync<UnityEngine.Object>(m_AssetReferences, callback);

        //    return result;
        //}
    }
    [Serializable]
    public sealed class AddressableAsset
    {
        [SerializeField] private string m_FriendlyName;
        [SerializeField] private AddressableReference m_AssetReference;

        public string FriendlyName { get => m_FriendlyName; set => m_FriendlyName = value; }
        public AddressableReference AssetReference => m_AssetReference;
#if UNITY_EDITOR
        public UnityEngine.Object EditorAsset => m_AssetReference.editorAsset;
#endif

        public AddressableAsset() : base() { }
#if UNITY_EDITOR
        public AddressableAsset(string name, UnityEditor.GUID guid) : this(name, guid.ToString()) { }
#endif
        public AddressableAsset(string name, string guid)
        {
            m_FriendlyName = name;
            m_AssetReference = new AddressableReference(guid);
        }
    }
}

#endif