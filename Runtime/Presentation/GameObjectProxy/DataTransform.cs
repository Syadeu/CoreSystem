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
        internal Vector3 m_LocalPosition;

        internal Vector3 m_EulerAngles;
        internal Vector3 m_LocalEulerAngles;
        internal quaternion m_Rotation;
        internal quaternion m_LocalRotation;

        internal Vector3 m_Right;
        internal Vector3 m_Up;
        internal Vector3 m_Forward;

        internal Vector3 m_LossyScale;
        internal Vector3 m_LocalScale;

        internal UnityEngine.Transform ProxyObject => PrefabManager.Instance.RecycleObjects[m_Idx.x].Instances[m_Idx.y].transform;

        public Vector3 position
        {
            get => m_Position;
            set
            {
                m_Position = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }
        public Vector3 localPosition
        {
            get => m_LocalPosition;
            set
            {
                m_LocalPosition = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }

        public Vector3 eulerAngles
        {
            get => m_EulerAngles;
            set
            {
                m_EulerAngles = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }
        public Vector3 localEulerAngles
        {
            get => m_LocalEulerAngles;
            set
            {
                m_LocalEulerAngles = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }
        public quaternion rotation
        {
            get => m_Rotation;
            set
            {
                m_Rotation = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }
        public quaternion localRotation
        {
            get => m_LocalRotation;
            set
            {
                m_LocalRotation = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }

        public Vector3 right
        {
            get => right;
            set
            {
                m_Right = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }
        public Vector3 up
        {
            get => up;
            set
            {
                m_Up = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }
        public Vector3 forward
        {
            get => m_Forward;
            set
            {
                m_Forward = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this;
            }
        }

        public Vector3 lossyScale => m_LossyScale;
        public Vector3 localScale
        {
            get => m_LocalScale;
            set
            {
                m_LocalScale = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Idx.x] = this; 
            }
        }
        Vector3 IReadOnlyTransform.position => m_Position;
        Vector3 IReadOnlyTransform.localPosition => m_LocalPosition;

        Vector3 IReadOnlyTransform.eulerAngles => m_EulerAngles;
        Vector3 IReadOnlyTransform.localEulerAngles => m_LocalEulerAngles;
        quaternion IReadOnlyTransform.rotation => m_Rotation;
        quaternion IReadOnlyTransform.localRotation => m_LocalRotation;

        Vector3 IReadOnlyTransform.right => m_Right;
        Vector3 IReadOnlyTransform.up => m_Up;
        Vector3 IReadOnlyTransform.forward => m_Forward;

        Vector3 IReadOnlyTransform.lossyScale => m_LossyScale;
        Vector3 IReadOnlyTransform.localScale => m_LocalScale;
    }
}
