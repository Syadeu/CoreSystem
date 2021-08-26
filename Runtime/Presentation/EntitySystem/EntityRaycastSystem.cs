using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class EntityRaycastSystem : PresentationSystemEntity<EntityRaycastSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private GameObjectProxySystem m_ProxySystem;
        private EntitySystem m_EntitySystem;
        private EntityBoundSystem m_BoundSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<GameObjectProxySystem>(Bind);
            RequestSystem<EntitySystem>(Bind);
            RequestSystem<EntityBoundSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_ProxySystem.OnDataObjectVisible -= M_ProxySystem_OnDataObjectVisible;
            m_ProxySystem.OnDataObjectInvisible -= M_ProxySystem_OnDataObjectInvisible;

            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;

            m_ProxySystem = null;
            m_EntitySystem = null;
            m_BoundSystem = null;
        }

        #region Binds

        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;

            m_ProxySystem.OnDataObjectVisible += M_ProxySystem_OnDataObjectVisible;
            m_ProxySystem.OnDataObjectInvisible += M_ProxySystem_OnDataObjectInvisible;
        }
        private void M_ProxySystem_OnDataObjectVisible(ProxyTransform obj)
        {
        }
        private void M_ProxySystem_OnDataObjectInvisible(ProxyTransform obj)
        {
        }

        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;

            m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
        }
        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            TriggerBoundAttribute att = obj.GetAttribute<TriggerBoundAttribute>();
            if (att == null) return;


        }

        private void Bind(EntityBoundSystem other)
        {
            m_BoundSystem = other;

        }

        #endregion

        public void Raycast(List<RaycastInfo> output, in Ray ray, in float maxDistance = -1)
        {
            output.Clear();
            for (int i = 0; i < m_BoundSystem.BoundCluster.Length; i++)
            {
                if (!m_BoundSystem.BoundCluster[i].IsCreated) return;

                ClusterGroup<TriggerBoundAttribute> group = m_BoundSystem.BoundCluster[i];
                if (!group.AABB.Intersect(ray, out float dis)) continue;

                if (maxDistance > 0 && dis > maxDistance) continue;

                for (int j = 0; j < group.Length; j++)
                {
                    if (!group.HasElementAt(j)) continue;

                    Entity<IEntity> entity = m_BoundSystem.TriggerBoundArray[group[j]];
                    if (!entity.transform.aabb.Intersect(ray, out dis, out float3 point)) continue;

                    if (maxDistance > 0 && dis > maxDistance) continue;

                    RaycastInfo info = new RaycastInfo(entity, true, dis, point);
                    output.Add(info);
                }
            }
        }
    }

    public readonly struct RaycastInfo
    {
        public readonly Entity<IEntity> entity;
        public readonly bool hit;
        
        public readonly float distance;
        public readonly float3 point;

        internal RaycastInfo(Entity<IEntity> a, bool b, float c, float3 d)
        {
            entity = a; hit = b; distance = c; point = d;
        }
    }
}
