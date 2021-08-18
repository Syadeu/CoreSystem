using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
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
        private Events.EventSystem m_EventSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_TriggerBoundCluster = new Cluster<TriggerBoundAttribute>(1024);
            m_TriggerBoundArray = new Entity<IEntity>[1024];

            RequestSystem<EntitySystem>(Bind);
            RequestSystem<Events.EventSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_TriggerBoundArray = Array.Empty<Entity<IEntity>>();
            m_TriggerBoundCluster.Dispose();

            m_EntitySystem = null;
            m_EventSystem = null;
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

            int arrayIndex = FindOrIncrementTriggerBoundArrayIndex();
            ClusterID id = m_TriggerBoundCluster.Add(entity.transform.position, arrayIndex);
            m_TriggerBoundArray[arrayIndex] = obj;

            att.m_ClusterID = id;
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            if (!(obj.Target is EntityBase)) return;
            TriggerBoundAttribute att = obj.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;

            int arrayIndex = m_TriggerBoundCluster.Remove(att.m_ClusterID);
            m_TriggerBoundArray[arrayIndex] = Entity<IEntity>.Empty;

            att.m_ClusterID = ClusterID.Empty;
        }

        private void Bind(Events.EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }

        #endregion

        #endregion

        #region Events

        private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        {
            TriggerBoundAttribute[] atts = ev.entity.GetAttributes<TriggerBoundAttribute>();
            if (atts == null) return;

            using (new CoreSystem.LogTimer(nameof(OnTransformChangedEventHandler), Channel.Debug))
            {
                for (int i = 0; i < atts.Length; i++)
                {
                    FindAndPostEvent(atts[i]);
                }
            }

            void FindAndPostEvent(TriggerBoundAttribute att)
            {
                var updatedID = m_TriggerBoundCluster.Update(att.m_ClusterID, ev.entity.transform.position);
                if (!updatedID.Equals(att.m_ClusterID))
                {
                    var prevGroup = m_TriggerBoundCluster.GetGroup(in att.m_ClusterID);
                    for (int i = 0; i < prevGroup.Length; i++)
                    {
                        if (!prevGroup.HasElementAt(i)) continue;

                        int arrIdx = prevGroup[i];
                        Entity<IEntity> target = m_TriggerBoundArray[arrIdx];

                        TryTrigger(in m_EventSystem, ev.entity, in target);
                    }
                }
                att.m_ClusterID = updatedID;
                ClusterGroup<TriggerBoundAttribute> group = m_TriggerBoundCluster.GetGroup(in att.m_ClusterID);
                
                for (int i = 0; i < group.Length; i++)
                {
                    if (i.Equals(att.m_ClusterID.ItemIndex) ||
                        !group.HasElementAt(i)) continue;

                    int arrIdx = group[i];
                    Entity<IEntity> target = m_TriggerBoundArray[arrIdx];

                    TryTrigger(in m_EventSystem, ev.entity, in target);
                }
            }
            static void TryTrigger(in EventSystem eventSystem, in Entity<IEntity> from, in Entity<IEntity> to)
            {
                TriggerBoundAttribute fromAtt = from.GetAttribute<TriggerBoundAttribute>();
                TriggerBoundAttribute toAtt = to.GetAttribute<TriggerBoundAttribute>();

                AABB fromAABB = fromAtt.m_MatchWithAABB ? ((IProxyTransform)from.transform).aabb : new AABB(fromAtt.m_Center + from.transform.position, fromAtt.m_Center);
                AABB toAABB = toAtt.m_MatchWithAABB ? ((IProxyTransform)to.transform).aabb : new AABB(toAtt.m_Center + to.transform.position, toAtt.m_Center);
                
                if (fromAABB.Intersect(toAABB))
                {
                    if (CanTriggerable(in fromAtt, in to) && !fromAtt.m_Triggered.Contains(to))
                    {
                        fromAtt.m_Triggered.Add(to);
                        eventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(from, to, true));
                    }
                    if (CanTriggerable(in toAtt, in from) && !toAtt.m_Triggered.Contains(from))
                    {
                        toAtt.m_Triggered.Add(from);
                        eventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(to, from, true));
                    }
                }
                else
                {
                    if (CanTriggerable(in fromAtt, in to) && fromAtt.m_Triggered.Contains(to))
                    {
                        fromAtt.m_Triggered.Remove(to);
                        eventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(from, to, false));
                    }
                    if (CanTriggerable(in toAtt, in from) && toAtt.m_Triggered.Contains(from))
                    {
                        toAtt.m_Triggered.Remove(from);
                        eventSystem.PostEvent(EntityTriggerBoundEvent.GetEvent(to, from, false));
                    }
                }
            }
            static bool CanTriggerable(in TriggerBoundAttribute att, in Entity<IEntity> target)
            {
                if (att.m_TriggerOnly.Length == 0) return true;

                for (int i = 0; i < att.m_TriggerOnly.Length; i++)
                {
                    if (att.m_TriggerOnly[i].Equals(target.Hash))
                    {
                        return !att.m_Inverse;
                    }
                }
                return att.m_Inverse;
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
}
