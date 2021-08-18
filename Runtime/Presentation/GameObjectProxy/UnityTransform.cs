using Syadeu.Presentation.Entities;
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
            get => provider.position;
            set => provider.position = value;
        }
        public quaternion rotation
        {
            get => provider.rotation;
            set => provider.rotation = value;
        }
        public float3 eulerAngles
        {
            get => provider.eulerAngles;
            set => provider.eulerAngles = value;
        }
        public float3 scale
        {
            get => provider.localScale;
            set => provider.localScale = value;
        }

        public float3 right => provider.right;
        public float3 up => provider.up;
        public float3 forward => provider.forward;

        public float4x4 localToWorldMatrix => provider.localToWorldMatrix;
        public float4x4 worldToLocalMatrix => provider.worldToLocalMatrix;

        public void Destroy()
        {
            PresentationSystem<EntitySystem>.System.DestroyEntity(entity);
        }

        public bool Equals(ITransform other)
        {
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
