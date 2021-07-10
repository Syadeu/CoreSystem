﻿using Syadeu.Database;
using Syadeu.Mono;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public struct DataMonoBehaviour : IDataComponent, IEquatable<DataMonoBehaviour>, IValidation
    {
        internal static int2 ProxyNull = new int2(-1, -1);
        internal static int2 ProxyQueued = new int2(-2, -2);

        internal Hash m_Idx;
        //internal int m_PrefabIdx;
        //internal int2 m_ProxyIdx;

        internal Hash m_Transform;
        

        Hash IDataComponent.Idx => m_Idx;
        DataComponentType IDataComponent.Type => DataComponentType.Component;
        bool IDataComponent.HasProxyObject => !InternalTransform.m_ProxyIdx.Equals(ProxyNull);
        bool IDataComponent.ProxyRequested => InternalTransform.m_ProxyIdx.Equals(ProxyQueued);
        bool IEquatable<IDataComponent>.Equals(IDataComponent other) => m_Idx.Equals(other.Idx);
        bool IEquatable<DataMonoBehaviour>.Equals(DataMonoBehaviour other) => m_Idx.Equals(other.m_Idx);

        internal RecycleableMonobehaviour ProxyObject
        {
            get
            {
                if (!((IDataComponent)this).HasProxyObject) return null;
                int2 proxyIdx = InternalTransform.m_ProxyIdx;
                return PrefabManager.Instance.RecycleObjects[proxyIdx.x].Instances[proxyIdx.y];
            }
        }
        private DataTransform InternalTransform => (DataTransform)PresentationSystem<GameObjectProxySystem>.System.m_MappedTransforms[m_Transform];
        public IReadOnlyTransform transform
            => /*(IReadOnlyTransform)PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Transform];*/
            throw new NotImplementedException();


        //public DataTransform GetTransform()
        //{
        //    GameObjectProxySystem proxySystem = PresentationSystem<GameObjectProxySystem>.System;
        //    DataTransform tr = (DataTransform)PresentationSystem<GameObjectProxySystem>.System.m_MappedData[m_Transform];
        //    if (!proxySystem.m_RequireUpdateQueuedList.Contains(tr))
        //    {
        //        proxySystem.m_RequireUpdateList.Enqueue(tr);
        //        proxySystem.m_RequireUpdateQueuedList.Add(tr);
        //    }
        //    return tr;
        //}
        //public void Terminate() => ProxyObject.Terminate();
        public bool IsValid() => !m_Idx.Equals(Hash.Empty);
    }
}
