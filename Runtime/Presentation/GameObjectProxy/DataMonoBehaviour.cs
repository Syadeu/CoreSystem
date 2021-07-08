using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.ThreadSafe;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public struct DataMonoBehaviour : IDataComponent, IEquatable<DataMonoBehaviour>, ITerminate, IValidation
    {
        internal Hash m_Hash;

        internal int3 m_Idx;
        internal int3 m_Transform;

        int3 IDataComponent.Idx => m_Idx;
        DataComponentType IDataComponent.Type => DataComponentType.Component;
        bool IEquatable<DataMonoBehaviour>.Equals(DataMonoBehaviour other) => m_Idx.Equals(other.m_Idx);

        internal RecycleableMonobehaviour ProxyObject => PrefabManager.Instance.RecycleObjects[m_Idx.x].Instances[m_Idx.y];
        public IReadOnlyTransform transform 
            => (IReadOnlyTransform)PresentationSystem<GameObjectProxySystem>.GetSystem().m_MappedData[m_Transform];


        public DataTransform GetTransform()
        {
            PresentationSystem<GameObjectProxySystem>.GetSystem().m_RequireUpdateList.Enqueue(this);
            return (DataTransform)PresentationSystem<GameObjectProxySystem>.GetSystem().m_MappedData[m_Transform];
        }
        public void Terminate() => ProxyObject.Terminate();
        public bool IsValid() => !m_Hash.Equals(Hash.Empty);
    }
    internal interface IDataComponent
    {
        /// <summary>
        /// x = prefabIdx, y = internalListIdx, z = DataComponentType
        /// </summary>
        int3 Idx { get; }
        DataComponentType Type { get; }

        IReadOnlyTransform transform { get; }
    }
    internal enum DataComponentType
    {
        Component,
        Transform,
    }
    public interface IReadOnlyTransform
    {
        Vector3 position { get; }
        Vector3 localPosition { get; }

        Vector3 eulerAngles { get; }
        Vector3 localEulerAngles { get; }
        quaternion rotation { get; }
        quaternion localRotation { get; }

        Vector3 right { get; }
        Vector3 up { get; }
        Vector3 forward { get; }

        Vector3 lossyScale { get; }
        Vector3 localScale { get; }
    }
    public struct DataTransform : IDataComponent, IReadOnlyTransform
    {
        internal int3 m_Idx;

        int3 IDataComponent.Idx => m_Idx;
        DataComponentType IDataComponent.Type => DataComponentType.Transform;
        IReadOnlyTransform IDataComponent.transform => this;

        public Vector3 position;
        public Vector3 localPosition;

        public Vector3 eulerAngles;
        public Vector3 localEulerAngles;
        public quaternion rotation;
        public quaternion localRotation;

        public Vector3 right;
        public Vector3 up;
        public Vector3 forward;

        public Vector3 lossyScale;
        public Vector3 localScale;

        internal UnityEngine.Transform ProxyObject => PrefabManager.Instance.RecycleObjects[m_Idx.x].Instances[m_Idx.y].transform;

        Vector3 IReadOnlyTransform.position => position;
        Vector3 IReadOnlyTransform.localPosition => localPosition;

        Vector3 IReadOnlyTransform.eulerAngles => eulerAngles;
        Vector3 IReadOnlyTransform.localEulerAngles => localEulerAngles;
        quaternion IReadOnlyTransform.rotation => rotation;
        quaternion IReadOnlyTransform.localRotation => localRotation;

        Vector3 IReadOnlyTransform.right => right;
        Vector3 IReadOnlyTransform.up => up;
        Vector3 IReadOnlyTransform.forward => forward;

        Vector3 IReadOnlyTransform.lossyScale => lossyScale;
        Vector3 IReadOnlyTransform.localScale => localScale;
    }
}
