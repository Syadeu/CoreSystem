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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Collections.ResourceControl
{
    public abstract class AssetReferenceLoaderBase<TObject> : CoreMonobehaviour
        where TObject : UnityEngine.Object
    {
        [Serializable]
        public sealed class Asset
        {
            [SerializeField] private AssetIndex m_Asset;
            [SerializeField] private UnityEvent<TObject> m_OnCompleted;

            [NonSerialized] private bool m_IsLoaded;
            [NonSerialized] private AsyncOperationHandle<TObject> m_LoadHandle;

            public bool TryLoadAsync(out AsyncOperationHandle<TObject> handle)
            {
                if (m_IsLoaded)
                {
                    handle = m_LoadHandle;
                    return true;
                }

                if (!m_Asset.IsValid())
                {
                    handle = ResourceManager.CreateCompletedOperationExeception<TObject>(null,
                        m_Asset, TypeHelper.TypeOf<TObject>.Type);
                    return false;
                }

                m_LoadHandle = m_Asset.AssetReference.LoadAssetAsync<TObject>();
                m_LoadHandle.Completed += M_LoadHandle_Completed;
                handle = m_LoadHandle;
                m_IsLoaded = true;

                return true;
            }
            private void M_LoadHandle_Completed(AsyncOperationHandle<TObject> obj)
            {
                TObject result = obj.Result;

                m_OnCompleted?.Invoke(result);
            }
            public void Release()
            {
                if (!m_IsLoaded) return;

                ResourceManager.Release(m_LoadHandle);

                m_IsLoaded = false;
            }
        }

        [SerializeField]
        protected Asset[] m_Assets = Array.Empty<Asset>();

        protected virtual void Awake()
        {
            AsyncOperationHandle<TObject> handle;
            for (int i = 0; i < m_Assets.Length; i++)
            {
                if (!m_Assets[i].TryLoadAsync(out handle))
                {
                    continue;
                }

                handle.Completed += M_LoadHandle_Completed;
            }
        }
        protected virtual void OnDestroy()
        {
            for (int i = 0; i < m_Assets.Length; i++)
            {
                m_Assets[i].Release();
            }
        }

        private void M_LoadHandle_Completed(AsyncOperationHandle<TObject> obj)
        {
            TObject result = obj.Result;

            OnLoadCompleted(result);
        }

        protected virtual void OnLoadCompleted(TObject obj) { }
    }
}

#endif