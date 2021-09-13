using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// Contains only instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Instance<T> : IEquatable<Instance<T>>
        where T : ObjectBase
    {
        public static readonly Instance<T> Empty = new Instance<T>(Hash.Empty);
        private static PresentationSystemID<EntitySystem> m_EntitySystem = PresentationSystemID<EntitySystem>.Null;

        private readonly Hash m_Idx;

        public Hash Idx => m_Idx;
        public T Object
        {
            get
            {
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

                if (!(m_EntitySystem.System.m_ObjectEntities[m_Idx] is T t))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Target is not a {TypeHelper.TypeOf<T>.Name}.");
                    return null;
                }

                return t;
            }
        }

        public Instance(Hash hash)
        {
            m_Idx = hash;
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

            m_Idx = obj.Idx;
        }

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(Instance<T> other) => m_Idx.Equals(other.m_Idx);

        public static Instance<T> CreateInstance(Reference<T> other)
        {
            if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(TypeHelper.TypeOf<T>.Type) ||
                TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(TypeHelper.TypeOf<T>.Type))
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You cannot create instance entity or attribute in this method.");
                return Empty;
            }
            return m_EntitySystem.System.CreateInstance(other);
        }
    }
}
