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

using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using Unity.Mathematics;
using UnityEngine;
using AABB = Syadeu.Collections.AABB;

namespace Syadeu.Presentation.Proxy
{
    public sealed class CustomTransform : IUnityTransform
    {
        public ConvertedEntity entity => throw new NotImplementedException();
        public Transform provider { get; private set; }
        private Renderer[] renderers { get; set; }

        bool ITransform.hasProxy => true;
        UnityEngine.Object ITransform.proxy => provider;
        public float3 position
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.position;
            }
            set
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                provider.position = value;
                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public quaternion rotation
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.rotation;
            }
            set
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                provider.rotation = value;
                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public float3 eulerAngles
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.eulerAngles;
            }
            set
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                provider.eulerAngles = value;
                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }
        public float3 scale
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.localScale;
            }
            set
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

                provider.localScale = value;
                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
            }
        }

        public float3 right
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.right;
            }
        }
        public float3 up
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.up;
            }
        }
        public float3 forward
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.forward;
            }
        }

        public float4x4 localToWorldMatrix
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.localToWorldMatrix;
            }
        }
        public float4x4 worldToLocalMatrix
        {
            get
            {
                if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");
                return provider.worldToLocalMatrix;
            }
        }

        public AABB aabb
        {
            get
            {
                if (renderers == null)
                {
                    renderers = provider.GetComponentsInChildren<Renderer>();
                }

                AABB temp = new AABB(position, float3.zero);
                for (int i = 0; i < renderers.Length; i++)
                {
                    temp.Encapsulate(renderers[i].bounds);
                }
                return temp;
            }
        }

        public CustomTransform(Transform tr)
        {
            provider = tr;
            renderers = tr.GetComponentsInChildren<Renderer>();
        }

        public void Destroy()
        {
            if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

            UnityEngine.Object.Destroy(provider.gameObject);
        }
        public bool Equals(ITransform other)
        {
            if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

            if (!(other is UnityTransform tr)) return false;
            return provider.Equals(tr.provider);
        }
        void IDisposable.Dispose()
        {
            provider = null;
        }
    }
}
