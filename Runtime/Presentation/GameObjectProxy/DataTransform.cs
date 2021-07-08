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
