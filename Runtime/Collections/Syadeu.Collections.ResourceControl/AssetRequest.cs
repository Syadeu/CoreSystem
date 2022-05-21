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

#if UNITY_2019_1_OR_NEWER
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

namespace Syadeu.Collections.ResourceControl
{
    public sealed class AssetRequest : IPromiseProvider<UnityEngine.Object>
    {
        public static AssetRequest Initialize(AssetBundleRequest request)
        {
            AssetRequest t = new AssetRequest();

            t.m_Request = request;

            t.m_Request.completed += t.OnRequestCompleted;

            return t;
        }

        private AssetBundleRequest m_Request;
        private Action<object> m_OnPromiseCompleted;
        private Action<UnityEngine.Object> m_OnPromiseTCompleted;
        private Action<AssetBundleRequest> m_OnRequestCompleted;

        public event Action<AssetBundleRequest> completed
        {
            add
            {
                if (m_Request.isDone)
                {
                    value?.Invoke(m_Request);
                    return;
                }

                m_OnRequestCompleted += value;
            }
            remove
            {
                if (m_Request.isDone) return;

                m_OnRequestCompleted -= value;
            }
        }

        private AssetRequest() { }
        ~AssetRequest()
        {
            m_Request = null;
            m_OnRequestCompleted = null;
        }

        private void OnRequestCompleted(AsyncOperation obj)
        {
            m_OnPromiseCompleted?.Invoke(m_Request);
            m_OnPromiseTCompleted?.Invoke(m_Request.asset);
            m_OnRequestCompleted?.Invoke(m_Request);
        }

        void IPromiseProvider<UnityEngine.Object>.OnComplete(Action<UnityEngine.Object> obj)
        {
            m_OnPromiseTCompleted += obj;
        }
    }
}

#endif