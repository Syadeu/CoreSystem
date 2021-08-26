using Syadeu.Internal;
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

        private EntityBoundSystem m_BoundSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<EntityBoundSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_BoundSystem = null;
        }

        #region Binds

        private void Bind(EntityBoundSystem other)
        {
            m_BoundSystem = other;
        }

        #endregion

        public bool Raycast(in Ray ray, out RaycastInfo info, in float maxDistance = -1)
        {
            info = RaycastInfo.Empty;

            for (int i = 0; i < m_BoundSystem.BoundCluster.Length; i++)
            {
                if (!m_BoundSystem.BoundCluster[i].IsCreated) continue;

                ClusterGroup<TriggerBoundAttribute> group = m_BoundSystem.BoundCluster[i];
                if (!group.AABB.Intersect(ray, out float dis)) continue;

                if (maxDistance > 0 && dis > maxDistance) continue;

                for (int j = 0; j < group.Length; j++)
                {
                    if (!group.HasElementAt(j)) continue;

                    Entity<IEntity> entity = m_BoundSystem.TriggerBoundArray[group[j]];
                    if (!entity.transform.aabb.Intersect(ray, out dis, out float3 point)) continue;

                    if (maxDistance > 0 && dis > maxDistance) continue;

                    if (dis < info.distance)
                    {
                        info = new RaycastInfo(entity, true, dis, point);
                    }
                }
            }

            return info.hit;
        }
        public void RaycastAll(List<RaycastInfo> output, in Ray ray, in float maxDistance = -1)
        {
            output.Clear();
            for (int i = 0; i < m_BoundSystem.BoundCluster.Length; i++)
            {
                if (!m_BoundSystem.BoundCluster[i].IsCreated) continue;

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
}
