using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    public abstract class DataComponentEntity : IInternalDataComponent, IValidation, IDisposable
    {
        const string c_WarningText = "This Data Component has been destoryed or didn\'t created propery. Request igonored.";

        internal Hash m_Idx;
        internal Hash m_GameObject;

        Hash IInternalDataComponent.GameObject => m_GameObject;
        Hash IInternalDataComponent.Idx => m_Idx;
        DataComponentType IInternalDataComponent.Type => DataComponentType.GameObject;
        bool IInternalDataComponent.HasProxyObject => !transform.m_ProxyIdx.Equals(DataTransform.ProxyNull);
        bool IInternalDataComponent.ProxyRequested => transform.m_ProxyIdx.Equals(DataTransform.ProxyQueued);
        bool IEquatable<IInternalDataComponent>.Equals(IInternalDataComponent other) => m_Idx.Equals(other.Idx);
        public bool IsValid() => !m_Disposed && !m_GameObject.Equals(Hash.Empty) && !m_Idx.Equals(Hash.Empty) &&
            PresentationSystem<GameObjectProxySystem>.System.m_ComponentList.ContainsKey(m_Idx) &&
            PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjectIdxes.ContainsKey(m_GameObject);

        private bool m_Disposed = false;

        public DataGameObject gameObject
        {
            get
            {
                unsafe
                {
                    if (!IsValid())
                    {
                        CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                        return default;
                    }
                    return *PresentationSystem<GameObjectProxySystem>.System.GetDataGameObjectPointer(m_GameObject);
                }
            }
        }
        public DataTransform transform
        {
            get
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return default;
                }
                return gameObject.transform;
            }
        }

        void IDisposable.Dispose()
        {
            OnDestory();
            m_Disposed = true;
        }
        protected virtual void OnDestory() { }
    }
}
