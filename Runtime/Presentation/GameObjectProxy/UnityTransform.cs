using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class UnityTransform : IUnityTransform
    {
        public ConvertedEntity entity { get; internal set; }
        public Transform provider { get; internal set; }

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
                PresentationSystem<EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
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
                PresentationSystem<EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
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
                PresentationSystem<EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
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
                PresentationSystem<EventSystem>.System.PostEvent(OnTransformChangedEvent.GetEvent(this));
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

        public void Destroy()
        {
            if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

            PresentationSystem<EntitySystem>.System.DestroyEntity(entity.AsReference<EntityDataBase, IEntity>());
        }

        public bool Equals(ITransform other)
        {
            if (provider == null) throw new CoreSystemException(CoreSystemExceptionFlag.Proxy, "Cannot access this transform because it is destroyed.");

            if (!(other is UnityTransform tr)) return false;
            return provider.Equals(tr.provider);
        }

        void IDisposable.Dispose()
        {
            entity = null;
            provider = null;
        }
    }
}
