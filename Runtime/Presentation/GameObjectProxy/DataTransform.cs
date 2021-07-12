using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.ThreadSafe;
using System;
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

        IReadOnlyTransform IInternalDataComponent.transform => this;
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
                if (!((IInternalDataComponent)this).HasProxyObject) return null;
                return PrefabManager.Instance.RecycleObjects[m_ProxyIdx.x].Instances[m_ProxyIdx.y];
            }
        }
        private DataTransform Data
        {
            get => (DataTransform)PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms[m_Idx];
            set => PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms[m_Idx] = value;
        }

        public Vector3 position
        {
            get => Data.m_Position;
            set
            {
                var tr = Data;
                tr.m_Position = value;
                Data = tr;
            }
        }
        public quaternion rotation
        {
            get => Data.m_Rotation;
            set
            {
                var tr = Data;
                tr.m_Rotation = value;
                Data = tr;
            }
        }
        public Vector3 right => throw new NotImplementedException();
        public Vector3 up => throw new NotImplementedException();
        public Vector3 forward => throw new NotImplementedException();

        public Vector3 localScale
        {
            get => Data.m_LocalScale;
            set
            {
                var tr = Data;
                tr.m_LocalScale = value;
                Data = tr;
            }
        }
        Vector3 IReadOnlyTransform.position => m_Position;
        quaternion IReadOnlyTransform.rotation => m_Rotation;

        Vector3 IReadOnlyTransform.right => m_Right;
        Vector3 IReadOnlyTransform.up => m_Up;
        Vector3 IReadOnlyTransform.forward => m_Forward;

        Vector3 IReadOnlyTransform.localScale => m_LocalScale;
    }
}
