using Syadeu.Mono;
using Syadeu.ThreadSafe;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public struct DataTransform : IDataComponent, IReadOnlyTransform
    {
        internal int3 m_Idx;

        int3 IDataComponent.Idx => m_Idx;
        DataComponentType IDataComponent.Type => DataComponentType.Transform;
        IReadOnlyTransform IDataComponent.transform => this;
        bool IEquatable<IDataComponent>.Equals(IDataComponent other) => m_Idx.Equals(other.Idx);

        internal Vector3 m_Position;
        internal quaternion m_Rotation;
        
        internal Vector3 m_Right;
        internal Vector3 m_Up;
        internal Vector3 m_Forward;

        internal Vector3 m_LocalScale;

        internal UnityEngine.Transform ProxyObject => PrefabManager.Instance.RecycleObjects[m_Idx.x].Instances[m_Idx.y].transform;
        private DataTransform Data
        {
            get => (DataTransform)PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx];
            set => PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx] = value;
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
