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

using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using AddressableReference = UnityEngine.AddressableAssets.AssetReference;

namespace Syadeu.Collections.ResourceControl
{
    [Serializable]
    public struct AssetReference : IValidation, IKeyEvaluator, IEmpty, IEquatable<AssetReference>
    {
        public static AssetReference Empty => new AssetReference();

        [SerializeField] private FixedString128Bytes m_Key;
        [SerializeField] private FixedString128Bytes m_SubAssetName;

        [JsonIgnore]
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
        [JsonIgnore]
        public AssetRuntimeKey RuntimeKey => new AssetRuntimeKey(FNV1a32.Calculate(((IKeyEvaluator)this).RuntimeKey.ToString()));
        [JsonIgnore]
        public bool IsSubAsset => !m_SubAssetName.IsEmpty;

        [JsonIgnore]
        public AsyncOperationHandle<IResourceLocation> Location => ResourceManager.GetLocation(this, TypeHelper.TypeOf<UnityEngine.Object>.Type);

        public AssetReference(FixedString128Bytes key) : this(key, default) { }
        public AssetReference(FixedString128Bytes key, FixedString128Bytes subAssetName)
        {
            m_Key = key;
            m_SubAssetName = subAssetName;

            //m_Handle = ResourceManager.CreateCompletedOperation<UnityEngine.Object>(
            //    null, new InvalidOperationException()
            //    );
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

        public Promise<UnityEngine.Object> LoadAssetAsync()
        {
            //if (m_Handle.IsValid()) return m_Handle.Convert<UnityEngine.Object>();

            var temp = ResourceManager.LoadAssetAsync<UnityEngine.Object>(this);
            //m_Handle = temp;

            return new AsyncOperationHandlePromise<UnityEngine.Object>(temp);
        }
        public Promise<TObject> LoadAssetAsync<TObject>()
            where TObject : UnityEngine.Object
        {
            //if (m_Handle.IsValid()) return m_Handle.Convert<TObject>();

            var temp = ResourceManager.LoadAssetAsync<TObject>(this);
            //m_Handle = temp;

            return new AsyncOperationHandlePromise<TObject>(temp);
        }

        public Promise<GameObject> InstantiateAsync(float3 position, quaternion rotation, Transform parent)
        {
            var temp = ResourceManager.InstantiateAsync(
                Location, new InstantiationParameters(position, rotation, parent));

            return new InstantiateAsyncOperationHandlePromise(temp);
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
    internal class AsyncOperationHandlePromise<TObject> : Promise<TObject>
    {
        private AsyncOperationHandle<TObject> m_Handle;

        protected AsyncOperationHandle<TObject> Handle => m_Handle;

        public AsyncOperationHandlePromise(AsyncOperationHandle<TObject> handle) : base()
        {
            m_Handle = handle;
            m_Handle.Completed += M_Handle_Completed;
        }
        private void M_Handle_Completed(AsyncOperationHandle<TObject> obj)
        {
            OnCompleteMethod(obj.Result);
        }

        protected override void OnDispose()
        {
            Addressables.Release(m_Handle);
        }
    }
    internal sealed class InstantiateAsyncOperationHandlePromise : AsyncOperationHandlePromise<GameObject>
    {
        public InstantiateAsyncOperationHandlePromise(AsyncOperationHandle<GameObject> handle) : base(handle)
        {
        }

        protected override void OnDispose()
        {
            Addressables.ReleaseInstance(Handle);

            base.OnDispose();
        }
    }
}

#endif