//using Syadeu.Database;
//using Syadeu.Mono;
//using Syadeu.Internal;
//using System;
//using Unity.Mathematics;

//namespace Syadeu.Presentation
//{
//    [Serializable]
//    public struct DataGameObject : IInternalDataComponent, IEquatable<DataGameObject>, IValidation, IDisposable
//    {
//        const string c_WarningText = "This Data GameObject has been destroyed or didn\'t created propery. Request ignored.";

//        public static DataGameObject Null = new DataGameObject() { m_Idx = Hash.Empty };

//        internal Hash m_Idx;
//        internal Hash m_Transform;
//        internal bool m_Destroyed;

//        unsafe private DataGameObject* GetPointer() => PresentationSystem<GameObjectProxySystem>.System.GetDataGameObjectPointer(m_Idx);
//        private ref DataGameObject GetRef()
//        {
//            unsafe
//            {
//                return ref *GetPointer();
//            }
//        }

//        Hash IInternalDataComponent.GameObject => m_Idx;
//        Hash IInternalDataComponent.Idx => m_Idx;
//        bool IInternalDataComponent.ProxyRequested => transform.ProxyRequested;
//        bool IEquatable<IInternalDataComponent>.Equals(IInternalDataComponent other) => m_Idx.Equals(other.Idx);
//        public bool Equals(DataGameObject other) => m_Idx.Equals(other.m_Idx);

//        public bool IsValid() =>
//            !m_Idx.Equals(Hash.Empty) && !m_Transform.Equals(Hash.Empty) &&
//            !PresentationSystem<GameObjectProxySystem>.System.Disposed &&
//            !GetRef().m_Destroyed &&
//            PresentationSystem<GameObjectProxySystem>.System.m_MappedTransformIdxes.ContainsKey(m_Transform) &&
//            PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjectIdxes.ContainsKey(m_Idx);

//        public bool HasProxyObject => transform.HasProxyObject;
//        public RecycleableMonobehaviour GetProxyObject()
//        {
//            if (!GetRef().IsValid() || !transform.HasProxyObject) return null;

//            return transform.ProxyObject;
//        }

//#pragma warning disable IDE1006 // Naming Styles
//#line hidden
//        public bool destroyed { get => m_Destroyed; private set => m_Destroyed = value; }

//        public DataTransform transform
//        {
//            get
//            {
//                if (!IsValid())
//                {
//                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
//                    return default;
//                }
//                unsafe
//                {
//                    return *PresentationSystem<GameObjectProxySystem>.System.GetDataTransformPointer(m_Transform);
//                }
//            }
//        }

//#line default
//#pragma warning restore IDE1006 // Naming Styles
//        public void Destroy()
//        {
//            if (this.Equals(Null))
//            {
//                CoreSystem.Logger.LogError(Channel.Proxy,
//                    "Cannot destroy null DataGameObject");
//                return;
//            }
//            if (destroyed)
//            {
//                CoreSystem.Logger.LogError(Channel.Proxy,
//                    $"Cannot destroy DataGameObject({m_Idx}) it\'s already destroyed.");
//                return;
//            }

//            //GetRef().m_Destroyed = true;
//            destroyed = true;
//            PresentationSystem<GameObjectProxySystem>.System.DestoryDataObject(m_Idx);
//        }

//        void IDisposable.Dispose()
//        {
//            ref DataGameObject obj = ref GetRef();
//            obj.m_Idx = Hash.Empty;
//            obj.m_Transform = Hash.Empty;
//        }

//        //internal void OnProxyCreated()
//        //{
//        //    if (!PresentationSystem<GameObjectProxySystem>.System.m_ComponentList.TryGetValue(m_Idx, out var list))
//        //    {
//        //        return;
//        //    }

//        //    for (int i = 0; i < list.Count; i++)
//        //    {
//        //        list[i].InternalOnProxyCreated();
//        //    }
//        //}
//        //internal void OnProxyRemoved()
//        //{
//        //    if (!PresentationSystem<GameObjectProxySystem>.System.m_ComponentList.TryGetValue(m_Idx, out var list))
//        //    {
//        //        return;
//        //    }

//        //    for (int i = 0; i < list.Count; i++)
//        //    {
//        //        list[i].InternalOnProxyRemoved();
//        //    }
//        //}
//    }
//}
