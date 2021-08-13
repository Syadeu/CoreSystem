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
        private TriggerBoundAttribute[] m_TriggerBoundArray;

        private EntitySystem m_EntitySystem;
        private Event.EventSystem m_EventSystem;

        protected override PresentationResult OnInitialize()
        {
            m_TriggerBoundCluster = new Cluster<TriggerBoundAttribute>(1024);
            m_TriggerBoundArray = new TriggerBoundAttribute[1024];

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

        private int FindOrIncrementTriggerBoundArrayIndex()
        {
            for (int i = 0; i < m_TriggerBoundArray.Length; i++)
            {
                if (m_TriggerBoundArray[i] == null) return i;
            }

            var newArr = new TriggerBoundAttribute[m_TriggerBoundArray.Length * 2];
            Array.Copy(m_TriggerBoundArray, newArr, m_TriggerBoundArray.Length);
            m_TriggerBoundArray = newArr;

            return FindOrIncrementTriggerBoundArrayIndex();
        }
        private void OnTransformChangedEvent(OnTransformChanged ev)
        {
            var att = ev.entity.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;

            att.m_ClusterID = m_TriggerBoundCluster.Update(att.m_ClusterID, ev.entity.transform.position);
        }
    }

    public sealed class EntityTriggerBoundEvent : SynchronizedEvent<EntityTriggerBoundEvent>
    {
        public Entity<IEntity> Source { get; private set; }
        public Entity<IEntity> Target { get; private set; }

        public EntityTriggerBoundEvent GetEvent(Entity<IEntity> source, Entity<IEntity> target)
        {
            var temp = Dequeue();

            Source = source;
            Target = target;

            return temp;
        }
        protected override void OnTerminate()
        {
            throw new NotImplementedException();
        }
    }
}
