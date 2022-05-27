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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using AddressableReference = UnityEngine.AddressableAssets.AssetReference;

namespace Syadeu.Collections.ResourceControl
{
    public sealed class ResourceHashMap : StaticScriptableObject<ResourceHashMap>
    {
        [Serializable]
        public sealed class SceneBindedLabel
        {
            [SerializeField] private SceneReference m_Scene;
            [SerializeField] private AssetLabelReference m_Label;

            [NonSerialized] private bool m_IsLoaded;
            [NonSerialized] AsyncOperationHandle<IList<IResourceLocation>> m_LocationOperation;
            [NonSerialized] AsyncOperationHandle<IList<UnityEngine.Object>> m_ResourceOperation;
            [NonSerialized] private Action<UnityEngine.Object> m_OnCompleted;

            private sealed class ObjectArrayProvider<TObject> : IPromiseProvider<IEnumerable<TObject>>
                where TObject : UnityEngine.Object
            {
                private SceneBindedLabel m_SceneBindedLabel;
                private Action<IEnumerable<TObject>> m_OnCompleted;

                public ObjectArrayProvider(SceneBindedLabel other)
                {
                    m_SceneBindedLabel = other;
                    m_SceneBindedLabel.OnCompleted += M_SceneBindedLabel_OnCompleted;
                }

                private void M_SceneBindedLabel_OnCompleted(UnityEngine.Object obj)
                {
                    m_OnCompleted?.Invoke(m_SceneBindedLabel.Result.Select(t => t as TObject));
                }

                void IPromiseProvider<IEnumerable<TObject>>.OnComplete(Action<IEnumerable<TObject>> obj)
                {
                    m_OnCompleted += obj;
                }
            }

            public bool IsLoaded => m_IsLoaded;
            public IList<UnityEngine.Object> Result
            {
                get
                {
                    if (!IsLoaded) return null;
                    else if (!m_ResourceOperation.IsDone) return null;

                    return m_ResourceOperation.Result;
                }
            }

            public event Action<UnityEngine.Object> OnCompleted
            {
                add
                {
                    if (!IsLoaded)
                    {
                        m_OnCompleted += value;
                        return;
                    }
                    else if (m_ResourceOperation.IsDone)
                    {
                        var result = m_ResourceOperation.Result;
                        for (int i = 0; i < result.Count; i++)
                        {
                            value?.Invoke(result[i]);
                        }
                        return;
                    }

                    m_OnCompleted += value;
                }
                remove
                {
                    m_OnCompleted -= value;
                }
            }

            public void Initialize()
            {
                if (!m_Label.RuntimeKeyIsValid())
                {
                    "?? error".ToLogError();
                    return;
                }

                m_LocationOperation = Addressables.LoadResourceLocationsAsync(m_Label);
            }

            public void LoadResources(Action<UnityEngine.Object> onCompleted)
            {
                if (IsLoaded)
                {
                    if (m_ResourceOperation.IsDone)
                    {
                        IList<UnityEngine.Object> objects = m_ResourceOperation.Result;
                        for (int i = 0; i < objects.Count; i++)
                        {
                            onCompleted?.Invoke(objects[i]);
                        }
                    }
                    else
                    {
                        m_OnCompleted += onCompleted;
                        m_ResourceOperation.Completed += M_ResourceOperation_Completed;
                    }
                    return;
                }

                m_OnCompleted += onCompleted;
                if (!m_LocationOperation.IsDone)
                {
                    m_LocationOperation.Completed += IfLocationOperation_IsNot_Completed;
                }
                else
                {
                    IList<IResourceLocation> locations = m_LocationOperation.Result;
                    m_ResourceOperation = Addressables.LoadAssetsAsync<UnityEngine.Object>(locations, null);

                    //var oper = Addressables.ResourceManager
                    //    .CreateChainOperation(
                    //        m_ResourceOperation,
                    //        RegisterResourceManagerOperation.Get
                    //        );


                    m_ResourceOperation.Completed += M_ResourceOperation_Completed;
                }

                m_IsLoaded = true;
            }
            public void UnloadResources()
            {
                if (!IsLoaded) return;

                Addressables.Release(m_ResourceOperation);
                Addressables.Release(m_LocationOperation);

                m_LocationOperation = default;
                m_ResourceOperation = default;
                m_OnCompleted = null;

                m_IsLoaded = false;
            }

            public bool Validate(Scene scene)
            {
                return m_Scene.Equals(scene);
            }
            public bool TryLoadResources(Scene scene, Action<UnityEngine.Object> onCompleted)
            {
                if (!m_Scene.Equals(scene))
                {
                    return false;
                }

                LoadResources(onCompleted);
                return true;
            }

            public Promise<IEnumerable<TObject>> GetObjects<TObject>()
                where TObject : UnityEngine.Object
            {
                var provider = new ObjectArrayProvider<TObject>(this);
                return new Promise<IEnumerable<TObject>>(provider);
            }

            private void IfLocationOperation_IsNot_Completed(AsyncOperationHandle<IList<IResourceLocation>> obj)
            {
                m_ResourceOperation = Addressables.LoadAssetsAsync<UnityEngine.Object>(obj.Result, null);
                m_ResourceOperation.Completed += M_ResourceOperation_Completed;
            }
            private void M_ResourceOperation_Completed(AsyncOperationHandle<IList<UnityEngine.Object>> obj)
            {
                IList<UnityEngine.Object> objects = obj.Result;
                for (int i = 0; i < objects.Count; i++)
                {
                    m_OnCompleted?.Invoke(objects[i]);
                }

                m_OnCompleted = null;
            }
        }
        
        [SerializeField] private List<SceneBindedLabel> m_SceneBindedLabels = new List<SceneBindedLabel>();
        [SerializeField] private List<ResourceList> m_ResourceLists = new List<ResourceList>();

        public IReadOnlyList<ResourceList> ResourceLists => m_ResourceLists;
        public ResourceList this[int index]
        {
            get
            {
                return m_ResourceLists[index];
            }
        }

        public AssetReference this[AssetIndex index]
        {
            get
            {
                return m_ResourceLists[index.m_Index.x][index.m_Index.y];
            }
        }
        public AssetReference this[string friendlyName]
        {
            get
            {
                AssetReference asset;
                foreach (var list in m_ResourceLists)
                {
                    asset = list[friendlyName];
                    if (asset.IsEmpty()) continue;

                    return asset;
                }

                return AssetReference.Empty;
            }
        }

        public bool TryGetAssetReference(AssetIndex index, out AssetReference asset)
        {
            asset = AssetReference.Empty;
            if (index.IsEmpty()) return false;

            int x = index.m_Index.x, y = index.m_Index.y;
            if (x < 0 || y < 0 || m_ResourceLists.Count <= x)
            {
                return false;
            }

            ResourceList list = m_ResourceLists[x];
            if (list.Count <= y)
            {
                return false;
            }

            asset = list[y];
            return !asset.IsEmpty();
        }

        internal void LoadSceneAssets(Scene scene, Action<UnityEngine.Object> onCompleted)
        {
            for (int i = 0; i < m_SceneBindedLabels.Count; i++)
            {
                if (!m_SceneBindedLabels[i].TryLoadResources(scene, null)) continue;

                m_SceneBindedLabels[i].OnCompleted += onCompleted;
                //
            }
            //
        }
        internal void UnloadSceneAssets(Scene scene)
        {
            for (int i = 0; i < m_SceneBindedLabels.Count; i++)
            {
                if (!m_SceneBindedLabels[i].Validate(scene)) continue;

                m_SceneBindedLabels[i].UnloadResources();
            }
        }
    }

    [Serializable]
    public struct GroupReference : IEmpty
    {
        public static GroupReference Empty = default(GroupReference);

        [SerializeField] private FixedString128Bytes m_Name;

        public GroupReference(FixedString128Bytes name)
        {
            m_Name = name;
        }

        public bool IsEmpty() => m_Name.IsEmpty;
        public override string ToString() => m_Name.ToString();

        public static implicit operator string(GroupReference t) => t.m_Name.ToString();
    }

    public readonly struct AssetRuntimeKey : IEmpty, IEquatable<AssetRuntimeKey>
    {
        private readonly uint m_Key;

        public AssetRuntimeKey(uint key)
        {
            m_Key = key;
        }

        public bool IsEmpty() => m_Key == 0;

        public bool Equals(AssetRuntimeKey other) => m_Key.Equals(other.m_Key);
        public override bool Equals(object obj)
        {
            if (!(obj is AssetRuntimeKey other)) return false;
            else if (!m_Key.Equals(other.m_Key)) return false;

            return true;
        }
        public override int GetHashCode() => unchecked((int)m_Key);
    }

    [Serializable]
    public struct AssetReference : IValidation, IKeyEvaluator, IEmpty, IEquatable<AssetReference>, IPromiseProvider<UnityEngine.Object>
    {
        public static AssetReference Empty => new AssetReference();

        [SerializeField] private FixedString128Bytes m_Key;
        [SerializeField] private FixedString128Bytes m_SubAssetName;

        [NonSerialized]
        AsyncOperationHandle m_Handle;

        object IKeyEvaluator.RuntimeKey
        {
            get
            {
                if (m_Key.IsEmpty) return string.Empty;

                const string c_Format = "{0}[{1}]";
                if (!m_SubAssetName.IsEmpty)
                {
                    return string.Format(c_Format, m_Key.ToString(), m_SubAssetName.ToString());
                }
                return m_Key.ToString();
            }
        }
        public AssetRuntimeKey RuntimeKey => new AssetRuntimeKey(FNV1a32.Calculate(((IKeyEvaluator)this).RuntimeKey.ToString()));
        public bool IsSubAsset => !m_SubAssetName.IsEmpty;

        public AsyncOperationHandle<IResourceLocation> Location => ResourceManager.GetLocation(this, TypeHelper.TypeOf<UnityEngine.Object>.Type);

        public AssetReference(FixedString128Bytes key) : this(key, default) { }
        public AssetReference(FixedString128Bytes key, FixedString128Bytes subAssetName)
        {
            m_Key = key;
            m_SubAssetName = subAssetName;

            m_Handle = ResourceManager.CreateCompletedOperation<UnityEngine.Object>(
                null, new InvalidOperationException()
                );
        }

        public bool IsEmpty()
        {
            return m_Key.IsEmpty || (m_Key.IsEmpty && m_SubAssetName.IsEmpty);
        }
        public bool IsValid()
        {
            const char c_guidstart = '[';

            if (m_Key.IsEmpty) return false;

            string text = ((IKeyEvaluator)this).RuntimeKey.ToString();
            int num = text.IndexOf(c_guidstart);
            if (num != -1)
            {
                text = text.Substring(0, num);
            }

            return Guid.TryParse(text, out _);
        }
        bool IKeyEvaluator.RuntimeKeyIsValid() => IsValid();
        void IPromiseProvider<UnityEngine.Object>.OnComplete(Action<UnityEngine.Object> obj)
        {
            AsyncOperationHandle<UnityEngine.Object> oper = LoadAssetAsync();
            oper.Completed += t =>
            {
                obj?.Invoke(t.Result);
                Addressables.Release(oper);
            };
        }

        public AsyncOperationHandle<UnityEngine.Object> LoadAssetAsync()
        {
            if (m_Handle.IsValid()) return m_Handle.Convert<UnityEngine.Object>();

            var temp = ResourceManager.LoadAssetAsync<UnityEngine.Object>(this);
            m_Handle = temp;

            return temp;
        }
        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>()
            where TObject : UnityEngine.Object
        {
            if (m_Handle.IsValid()) return m_Handle.Convert<TObject>();

            var temp = ResourceManager.LoadAssetAsync<TObject>(this);
            m_Handle = temp;

            return temp;
        }

        public bool Equals(AssetReference other) => m_Key.Equals(other.m_Key);
        public override string ToString()
        {
            if (IsEmpty()) return "Invalid";
            else if (IsSubAsset) return $"{m_Key}[{m_SubAssetName}]";
            return m_Key.ToString();
        }

        public static implicit operator AssetReference(AddressableReference t)
        {
            if (t.AssetGUID.IsNullOrEmpty()) return Empty;
            else if (t.SubObjectName.IsNullOrEmpty())
            {
                return new AssetReference(t.AssetGUID);
            }
            return new AssetReference(t.AssetGUID, t.SubObjectName);
        }
        public static implicit operator AssetReference(string t)
        {
            Match match = Regex.Match(t, @"(^.+)" + Regex.Escape("[") + @"(.+)]");
            if (match.Success)
            {
                return new AssetReference(match.Groups[1].Value, match.Groups[2].Value);
            }
            return new AssetReference(t);
        }
    }
}

#endif