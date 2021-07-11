using Syadeu.Database;
using Syadeu.Mono;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    [Serializable]
    public struct DataGameObject : IInternalDataComponent, IEquatable<DataGameObject>, IDisposable, ITag
    {
        internal UserTagFlag m_UserTag;
        internal CustomTagFlag m_CustomTag;

        internal Hash m_Idx;
        internal Hash m_Transform;

        public UserTagFlag UserTag
        {
            get => m_UserTag;
            set
            {
                var boxed = PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjects[m_Idx];
                boxed.m_UserTag = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjects[m_Idx] = boxed;
            }
        }
        public CustomTagFlag CustomTag
        {
            get => m_CustomTag;
            set
            {
                var boxed = PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjects[m_Idx];
                boxed.m_CustomTag = value;
                PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjects[m_Idx] = boxed;
            }
        }

        Hash IInternalDataComponent.Idx => m_Idx;
        DataComponentType IInternalDataComponent.Type => DataComponentType.GameObject;
        bool IInternalDataComponent.HasProxyObject => !InternalTransform.m_ProxyIdx.Equals(DataTransform.ProxyNull);
        bool IInternalDataComponent.ProxyRequested => InternalTransform.m_ProxyIdx.Equals(DataTransform.ProxyQueued);
        bool IEquatable<IInternalDataComponent>.Equals(IInternalDataComponent other) => m_Idx.Equals(other.Idx);
        bool IEquatable<DataGameObject>.Equals(DataGameObject other) => m_Idx.Equals(other.m_Idx);

        internal DataTransform InternalTransform => PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms[m_Transform];
        public IReadOnlyTransform transform => InternalTransform;

        public T AddComponent<T>() where T : DataComponentEntity, new()
        {
            if (!PresentationSystem<GameObjectProxySystem>.System.m_ComponentList.TryGetValue(m_Idx, out var list))
            {
                list = new System.Collections.Generic.List<DataComponentEntity>();
                PresentationSystem<GameObjectProxySystem>.System.m_ComponentList.Add(m_Idx, list);
            }
            T t = new T();
            list.Add(t);
            return t;
        }
        public T GetComponent<T>() where T : DataComponentEntity, new()
        {
            if (!PresentationSystem<GameObjectProxySystem>.System.m_ComponentList.TryGetValue(m_Idx, out var list))
            {
                return null;
            }
            return (T)list.FindFor((other) => other is T);
        }
        public void RemoveComponent<T>(T t) where T : DataComponentEntity
        {
            if (!PresentationSystem<GameObjectProxySystem>.System.m_ComponentList.TryGetValue(m_Idx, out var list))
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(t))
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        void IDisposable.Dispose() { }
        public void Destory()
        {
            PresentationSystem<GameObjectProxySystem>.System.DestoryDataObject(m_Idx);
        }
    }
}
