using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    /// <summary>
    /// Contains only instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Instance<T> : IInstance, IEquatable<Instance<T>>
        where T : class, IObject
    {
        public static readonly Instance<T> Empty = new Instance<T>(Hash.Empty);
        private static PresentationSystemID<EntitySystem> m_EntitySystem = PresentationSystemID<EntitySystem>.Null;

        private Hash m_Idx;

        public Hash Idx => m_Idx;

        IObject IInstance.Object => Object;
        public T Object
        {
            get
            {
                if (IsEmpty())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                            "Cannot retrived null instance.");
                    return null;
                }

                if (m_EntitySystem.IsNull())
                {
                    m_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                    if (m_EntitySystem.IsNull())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            "Cannot retrived EntitySystem.");
                        return null;
                    }
                }

                if (!m_EntitySystem.System.m_ObjectEntities.TryGetValue(m_Idx, out ObjectBase obj))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                            $"Target({m_Idx}) is not exist.");
                    return null;
                }

                if (!(obj is T t))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Target is not a {TypeHelper.TypeOf<T>.Name}.");
                    return null;
                }

                return t;
            }
        }

        public Instance(Hash idx)
        {
            m_Idx = idx;
        }
        public Instance(ObjectBase obj)
        {
            if (obj.Idx.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Object({obj.Name}) is not an instance.");
                m_Idx = Hash.Empty;
                return;
            }
            if (!(obj is T))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Object({obj.Name}) is not a {TypeHelper.TypeOf<T>.Name}.");
                m_Idx = Hash.Empty;
                return;
            }

            m_Idx = obj.Idx;
        }

        public bool IsValid()
        {
            if (IsEmpty()) return false;
            else if (m_EntitySystem.IsNull())
            {
                m_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                if (m_EntitySystem.IsNull())
                {
                    return false;
                }
            }
            else if (!(m_EntitySystem.System.m_ObjectEntities[m_Idx] is T))
            {
                return false;
            }

            return true;
        }
        public bool IsEmpty() => Equals(Empty);
        public bool Equals(Instance<T> other) => m_Idx.Equals(other.m_Idx);
        public bool Equals(IInstance other) => m_Idx.Equals(other.Idx);

        public void Destroy()
        {
            if (IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "Cannot destroy an empty instance");
                return;
            }

            m_EntitySystem.System.DestroyObject(this);
            m_Idx = Hash.Empty;
        }

        public static Instance<T> CreateInstance(Reference<T> other)
        {
            if (m_EntitySystem.IsNull())
            {
                m_EntitySystem = PresentationSystem<EntitySystem>.SystemID;
                if (m_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return Empty;
                }
            }

            if (TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(TypeHelper.TypeOf<T>.Type))
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You cannot create instance of attribute.");
                return Empty;
            }

            if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(TypeHelper.TypeOf<T>.Type))
            {
                Instance<IEntity> ins = m_EntitySystem.System.CreateEntity(other, float3.zero).AsInstance();
                return ins.Cast<IEntity, T>();
            }

            return m_EntitySystem.System.CreateInstance(other);
        }
    }
}
