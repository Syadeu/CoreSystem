using Syadeu.Database;
using Syadeu.Mono;
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
        bool IEquatable<IDataComponent>.Equals(IDataComponent other) => m_Idx.Equals(other.Idx);
        bool IEquatable<DataMonoBehaviour>.Equals(DataMonoBehaviour other) => m_Idx.Equals(other.m_Idx);

        internal RecycleableMonobehaviour ProxyObject => PrefabManager.Instance.RecycleObjects[m_Idx.x].Instances[m_Idx.y];
        public IReadOnlyTransform transform 
            => (IReadOnlyTransform)PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Transform];


        public DataTransform GetTransform()
        {
            GameObjectProxySystem proxySystem = PresentationSystem<GameObjectProxySystem>.System;
            DataTransform tr = (DataTransform)PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Transform];
            if (!proxySystem.m_RequireUpdateQueuedList.Contains(tr))
            {
                proxySystem.m_RequireUpdateList.Enqueue(tr);
                proxySystem.m_RequireUpdateQueuedList.Add(tr);
            }
            return tr;
        }
        public void Terminate() => ProxyObject.Terminate();
        public bool IsValid() => !m_Hash.Equals(Hash.Empty);
    }
}
