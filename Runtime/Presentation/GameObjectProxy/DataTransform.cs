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
                if (!((IInternalDataComponent)this).HasProxyObject) return null;
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

        public Vector3 position
        {
            get => m_Position;
            set
            {
                ref DataTransform tr = ref GetRef();
                tr.m_Position = value;
            }
        }
        public quaternion rotation
        {
            get => m_Rotation;
            set
            {
                ref DataTransform tr = ref GetRef();
                tr.m_Rotation = value;
            }
        }
        public Vector3 right => throw new NotImplementedException();
        public Vector3 up => throw new NotImplementedException();
        public Vector3 forward => throw new NotImplementedException();

        public Vector3 localScale
        {
            get => m_LocalScale;
            set
            {
                ref DataTransform tr = ref GetRef();
                tr.m_LocalScale = value;
            }
        }
        Vector3 IReadOnlyTransform.position => m_Position;
        quaternion IReadOnlyTransform.rotation => m_Rotation;

        Vector3 IReadOnlyTransform.right => m_Right;
        Vector3 IReadOnlyTransform.up => m_Up;
        Vector3 IReadOnlyTransform.forward => m_Forward;

        Vector3 IReadOnlyTransform.localScale => m_LocalScale;

        //unsafe private void Test()
        //{
        //    //PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms.ToLookup((other) => ).

        //    PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms.geta

        //    //PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms.GetBucketData().
        //    UnsafeUtility.AddressOf(ref PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms[m_Idx]);

        //    ref var tr = ref PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms[m_Idx];
        //}
    }
}
