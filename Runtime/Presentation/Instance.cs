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
                    m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
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
                m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
                if (m_EntitySystem.IsNull())
                {
                    return false;
                }
            }
            else if (m_EntitySystem.System == null || 
                !(m_EntitySystem.System.m_ObjectEntities[m_Idx] is T))
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
                m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
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

            if (TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(TypeHelper.TypeOf<T>.Type))
            {
                Instance<IEntity> ins = m_EntitySystem.System.CreateEntity(other, float3.zero).AsInstance();
                return ins.Cast<IEntity, T>();
            }
            else if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(TypeHelper.TypeOf<T>.Type))
            {
                Instance<IEntityData> ins = m_EntitySystem.System.CreateObject(other).AsInstance();
                return ins.Cast<IEntityData, T>();
            }

            return m_EntitySystem.System.CreateInstance(other);
        }
        public static Instance<TA> CreateInstance<TA>(Reference<TA> other, float3 pos, quaternion rot, float3 localScale)
            where TA : class, IEntity
        {
            if (m_EntitySystem.IsNull())
            {
                m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
                if (m_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return Instance<TA>.Empty;
                }
            }

            Instance<IEntity> ins = m_EntitySystem.System.CreateEntity(other, in pos, in rot, in localScale).AsInstance();
            return ins.Cast<IEntity, TA>();
        }
    }

    public struct Instance : IInstance, IEquatable<Instance>
    {
        public static readonly Instance Empty = new Instance(Hash.Empty);
        private static PresentationSystemID<EntitySystem> m_EntitySystem = PresentationSystemID<EntitySystem>.Null;

        private Hash m_Idx;

        public Hash Idx => m_Idx;

        public IObject Object
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
                    m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
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

                return obj;
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

            m_Idx = obj.Idx;
        }

        public bool IsValid()
        {
            if (IsEmpty()) return false;
            else if (m_EntitySystem.IsNull())
            {
                m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
                if (m_EntitySystem.IsNull())
                {
                    return false;
                }
            }

            return true;
        }
        public bool IsEmpty() => Equals(Empty);
        public bool Equals(Instance other) => m_Idx.Equals(other.m_Idx);
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

        public static Instance CreateInstance(Reference other)
        {
            if (other.IsEmpty() || !other.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    "Invalid reference.");
                return Empty;
            }
            else if (m_EntitySystem.IsNull())
            {
                m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
                if (m_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return Empty;
                }
            }

            Type type = other.GetObject().GetType();

            if (TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(type))
            {
                CoreSystem.Logger.LogError(Channel.Entity, "You cannot create instance of attribute.");
                return Empty;
            }

            if (TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(type))
            {
                Entity<IEntity> ins = m_EntitySystem.System.CreateEntity(other, float3.zero);
                return new Instance(ins.Idx);
            }
            else if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(type))
            {
                EntityData<IEntityData> ins = m_EntitySystem.System.CreateObject(other);
                return new Instance(ins.Idx);
            }

            return m_EntitySystem.System.CreateInstance(other);
        }
        public static Instance<TA> CreateInstance<TA>(Reference<TA> other, float3 pos, quaternion rot, float3 localScale)
            where TA : class, IEntity
        {
            if (m_EntitySystem.IsNull())
            {
                m_EntitySystem = PresentationSystem<DefaultPresentationGroup, EntitySystem>.SystemID;
                if (m_EntitySystem.IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        "Cannot retrived EntitySystem.");
                    return Instance<TA>.Empty;
                }
            }

            Instance<IEntity> ins = m_EntitySystem.System.CreateEntity(other, in pos, in rot, in localScale).AsInstance();
            return ins.Cast<IEntity, TA>();
        }

    }
}
