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

#if UNITY_2020_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Syadeu.Collections.ResourceControl.LowLevel
{
    internal sealed unsafe class AssetBundleLoadAsyncHandler : IPromiseProvider<AssetBundle>
    {
        private UnsafeAssetBundleInfo* m_Bundle;
        private ResourceManager.AssetContainer m_AssetContainer;

        private UnityWebRequest m_WebRequest;
        private Action<AssetBundle> m_OnComplete;

        public AsyncOperation Initialize(UnsafeAssetBundleInfo* bundle, ResourceManager.AssetContainer container, UnityWebRequest webRequest)
        {
            m_Bundle = bundle;
            m_AssetContainer = container;

            m_WebRequest = webRequest;
            var request = m_WebRequest.SendWebRequest();
            request.completed += M_WebRequest_completed;

            return request;
        }

        void IPromiseProvider<AssetBundle>.OnComplete(Action<AssetBundle> obj)
        {
            m_OnComplete += obj;
        }

        private void M_WebRequest_completed(UnityEngine.AsyncOperation obj)
        {
            m_Bundle->loaded = true;
            //byte[] assetBundleBytes = m_WebRequest.downloadHandler.data;
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(m_WebRequest);

            ResourceManager.GetAssetBundle(m_Bundle->index).AssetBundle = bundle;
            ResourceManager.UpdateAssetInfos(m_Bundle, bundle);

            m_OnComplete?.Invoke(bundle);
        }
    }
}

#endif