using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.ThreadSafe;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public struct DataTransform : IInternalDataComponent, IReadOnlyTransform, IEquatable<DataTransform>, IDisposable
    {
        internal static int2 ProxyNull = new int2(-1, -1);
        internal static int2 ProxyQueued = new int2(-2, -2);

        internal Hash m_GameObject;
        internal Hash m_Idx;
        internal int2 m_ProxyIdx;
        internal int m_PrefabIdx;
        internal bool m_EnableCull;

        Hash IInternalDataComponent.GameObject => m_GameObject;
        Hash IInternalDataComponent.Idx => m_Idx;
        DataComponentType IInternalDataComponent.Type => DataComponentType.Transform;
        bool IInternalDataComponent.HasProxyObject => !m_ProxyIdx.Equals(ProxyNull);
        internal bool HasProxyObject => !m_ProxyIdx.Equals(ProxyNull);
        bool IInternalDataComponent.ProxyRequested => m_ProxyIdx.Equals(ProxyQueued);
        internal bool ProxyRequested => m_ProxyIdx.Equals(ProxyQueued);

        void IDisposable.Dispose() { }

        DataTransform IInternalDataComponent.transform => this;
        bool IEquatable<IInternalDataComponent>.Equals(IInternalDataComponent other) => m_Idx.Equals(other.Idx);
        bool IEquatable<DataTransform>.Equals(DataTransform other) => m_Idx.Equals(other.m_Idx);

        internal Vector3 m_Position;
        internal quaternion m_Rotation;

        internal Vector3 m_Right;
        internal Vector3 m_Up;
        internal Vector3 m_Forward;

        internal Vector3 m_LocalScale;

        internal RecycleableMonobehaviour ProxyObject
        {
            get
            {
                if (!((IInternalDataComponent)this).HasProxyObject || ((IInternalDataComponent)this).ProxyRequested) return null;
                return PresentationSystem<GameObjectProxySystem>.System.m_Instances[m_ProxyIdx.x][m_ProxyIdx.y];
            }
        }
        unsafe private DataTransform* GetPointer() => PresentationSystem<GameObjectProxySystem>.System.GetDataTransformPointer(m_Idx);
        private ref DataTransform GetRef()
        {
            unsafe
            {
                return ref *GetPointer();
            }
        }
        private void RequestUpdate()
        {
            if (!PresentationSystem<RenderSystem>.System.IsInCameraScreen(m_Position)) return;
            PresentationSystem<GameObjectProxySystem>.System.RequestUpdateTransform(m_Idx);
        }

#pragma warning disable IDE1006 // Naming Styles
        public Vector3 position
        {
            get => GetRef().m_Position;
            set
            {
                ref DataTransform tr = ref GetRef();
                if (tr.m_Position.Equals(value)) return;
                tr.m_Position = value;
                RequestUpdate();
            }
        }
        public Vector3 eulerAngles
        {
            get => new Vector3(rotation.Euler());
            set
            {
                rotation = QuaternionExtensions.FromAngles(new float3(value.x, value.y, value.z));
            }
        }
        public quaternion rotation
        {
            get => GetRef().m_Rotation;
            set
            {
                ref DataTransform tr = ref GetRef();
                if (tr.m_Rotation.Equals(value)) return;
                tr.m_Rotation = value;
                RequestUpdate();
            }
        }
        public Vector3 right => GetRef().right;
        public Vector3 up => GetRef().up;
        public Vector3 forward => GetRef().forward;

        public Vector3 localScale
        {
            get => GetRef().m_LocalScale;
            set
            {
                ref DataTransform tr = ref GetRef();
                if (tr.m_LocalScale.Equals(value)) return;
                tr.m_LocalScale = value;
                RequestUpdate();
            }
        }
        Vector3 IReadOnlyTransform.position => GetRef().m_Position;
        Vector3 IReadOnlyTransform.eulerAngles => GetRef().eulerAngles;
        quaternion IReadOnlyTransform.rotation => GetRef().m_Rotation;

        Vector3 IReadOnlyTransform.right => GetRef().m_Right;
        Vector3 IReadOnlyTransform.up => GetRef().m_Up;
        Vector3 IReadOnlyTransform.forward => GetRef().m_Forward;

        Vector3 IReadOnlyTransform.localScale => GetRef().m_LocalScale;
#pragma warning restore IDE1006 // Naming Styles
    }
}
