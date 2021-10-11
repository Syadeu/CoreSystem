#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class EntityRaycastSystem : PresentationSystemEntity<EntityRaycastSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private UnsafeMultiHashMap<LayerInfo, Entity<IEntity>> m_LayerMap;

        private EntityBoundSystem m_BoundSystem;

        private readonly struct LayerInfo : IEquatable<LayerInfo>
        {
            private readonly ulong m_Hash;

            public LayerInfo(Reference<TriggerBoundLayer> layer)
            {
                var obj = layer.GetObject();
                ulong hash = obj.Hash;
                unchecked
                {
                    m_Hash = hash * 397 ^ FNV1a32.Calculate(obj.m_LayerGroup);
                }
            }
            public bool Equals(LayerInfo other) => m_Hash.Equals(other.m_Hash);
        }

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EntityBoundSystem>(Bind);

            m_LayerMap = new UnsafeMultiHashMap<LayerInfo, Entity<IEntity>>(512, AllocatorManager.Persistent);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_LayerMap.Dispose();

            m_BoundSystem = null;
        }

        #region Binds

        private void Bind(EntityBoundSystem other)
        {
            m_BoundSystem = other;
        }

        #endregion

        public void AddLayerEntity(Reference<TriggerBoundLayer> layer, Entity<IEntity> entity)
        {
            if (layer.IsEmpty()) return;

            m_LayerMap.Add(new LayerInfo(layer), entity);
        }
        public void RemoveLayerEntity(Reference<TriggerBoundLayer> layer, Entity<IEntity> entity)
        {
            if (layer.IsEmpty()) return;

            m_LayerMap.Remove(new LayerInfo(layer), entity);
        }

        /// <summary>
        /// 레이캐스트를 실행합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="TriggerBoundAttribute"/>가 있는 <see cref="Entity{T}"/>만 검출합니다. 
        /// 엔티티 이외에는 <seealso cref="Physics.Raycast(Ray)"/>를 사용하세요.
        /// </remarks>
        /// <param name="ray"></param>
        /// <param name="info"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
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
        /// <summary>
        /// <inheritdoc cref="Raycast(in Ray, out RaycastInfo, in float)"/>
        /// </summary>
        /// <remarks>
        /// 경로상에 있는 모든 <see cref="Entity{T}"/>를 검출하여 <paramref name="output"/>에 담습니다.
        /// 배열은 메소드 실행시 자동으로 초기화됩니다.
        /// </remarks>
        /// <param name="output"></param>
        /// <param name="ray"></param>
        /// <param name="maxDistance"></param>
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
