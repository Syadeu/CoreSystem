using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation
{
    public sealed class EntityTriggerSystem : PresentationSystemEntity<EntityTriggerSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private Cluster<TriggerBoundAttribute> m_TriggerBoundCluster;
        private Entity<IEntity>[] m_TriggerBoundArray;

        private EntitySystem m_EntitySystem;
        private Event.EventSystem m_EventSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_TriggerBoundCluster = new Cluster<TriggerBoundAttribute>(1024);
            m_TriggerBoundArray = new Entity<IEntity>[1024];

            RequestSystem<EntitySystem>(Bind);
            RequestSystem<Event.EventSystem>(Bind);

            return base.OnInitialize();
        }

        #region Bind

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;

            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
        }
        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            if (!(obj.Target is EntityBase entity)) return;
            TriggerBoundAttribute att = obj.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;

            ClusterID id = m_TriggerBoundCluster.Add(entity.transform.position, FindOrIncrementTriggerBoundArrayIndex());

            att.m_ClusterID = id;
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            if (!(obj.Target is EntityBase)) return;
            TriggerBoundAttribute att = obj.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;

            int arrayIndex = m_TriggerBoundCluster.Remove(att.m_ClusterID);
            m_TriggerBoundArray[arrayIndex] = null;

            att.m_ClusterID = ClusterID.Empty;
        }

        private void Bind(Event.EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChanged>(OnTransformChangedEvent);
        }

        #endregion

        #endregion

        #region Events

        private void OnTransformChangedEvent(OnTransformChanged ev)
        {
            var att = ev.entity.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;

            att.m_ClusterID = m_TriggerBoundCluster.Update(att.m_ClusterID, ev.entity.transform.position);
            ClusterGroup<TriggerBoundAttribute> group = m_TriggerBoundCluster.GetGroup(in att.m_ClusterID);
            AABB fromAABB = ev.transform.aabb;

            for (int i = 0; i < group.Length; i++)
            {
                if (i.Equals(att.m_ClusterID.ItemIndex) ||
                    !group.HasElementAt(i)) continue;

                int arrIdx = group[i];
                Entity<IEntity> target = m_TriggerBoundArray[arrIdx];

                if (!target.transform.aabb.Intersect(fromAABB)) continue;

                m_EventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(ev.entity, target));
            }
        }

        #endregion

        private int FindOrIncrementTriggerBoundArrayIndex()
        {
            for (int i = 0; i < m_TriggerBoundArray.Length; i++)
            {
                if (m_TriggerBoundArray[i].Equals(Entity<IEntity>.Empty)) return i;
            }

            Entity<IEntity>[] newArr = new Entity<IEntity>[m_TriggerBoundArray.Length * 2];
            Array.Copy(m_TriggerBoundArray, newArr, m_TriggerBoundArray.Length);
            m_TriggerBoundArray = newArr;

            return FindOrIncrementTriggerBoundArrayIndex();
        }
    }

    public sealed class EntityTriggerBoundEvent : SynchronizedEvent<EntityTriggerBoundEvent>
    {
        public Entity<IEntity> Source { get; private set; }
        public Entity<IEntity> Target { get; private set; }

        public static EntityTriggerBoundEvent GetEvent(Entity<IEntity> source, Entity<IEntity> target)
        {
            var temp = Dequeue();

            temp.Source = source;
            temp.Target = target;

            return temp;
        }
        protected override void OnTerminate()
        {
            Source = Entity<IEntity>.Empty;
            Target = Entity<IEntity>.Empty;
        }
    }
}
