using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    public abstract class DataComponentEntity : IInternalDataComponent, IDisposable
    {
        internal Hash m_Idx;
        internal Hash m_GameObject;

        Hash IInternalDataComponent.GameObject => m_GameObject;
        Hash IInternalDataComponent.Idx => m_Idx;
        DataComponentType IInternalDataComponent.Type => DataComponentType.GameObject;
        bool IInternalDataComponent.HasProxyObject => !InternalTransform.m_ProxyIdx.Equals(DataTransform.ProxyNull);
        bool IInternalDataComponent.ProxyRequested => InternalTransform.m_ProxyIdx.Equals(DataTransform.ProxyQueued);
        bool IEquatable<IInternalDataComponent>.Equals(IInternalDataComponent other) => m_Idx.Equals(other.Idx);

        public DataGameObject gameObject => PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjects[m_GameObject];
        private DataTransform InternalTransform => gameObject.InternalTransform;
        public IReadOnlyTransform transform => InternalTransform;

        public virtual void Dispose() { }
    }
}
