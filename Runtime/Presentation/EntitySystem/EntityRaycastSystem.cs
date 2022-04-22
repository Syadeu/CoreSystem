// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    // https://forum.unity.com/threads/raycast-without-colliders.14378/
    // https://forum.unity.com/threads/a-solution-for-accurate-raycasting-without-mesh-colliders.134554/

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
        protected override void OnDispose()
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

        // https://answers.unity.com/questions/1246539/why-does-a-spherecast-require-a-direction-shouldnt.html
        public bool SphereCast(float3 origin, float radius, float3 direction, out RaycastInfo info, in float maxDistance = float.MaxValue)
        {
            info = RaycastInfo.Empty;

            if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit, maxDistance))
            {
                var monoObj = hit.collider.GetComponent<RecycleableMonobehaviour>();
                if (monoObj != null)
                {
                    info = new RaycastInfo(monoObj.entity, true, hit.distance, hit.point);

                    return true;
                }
            }
            return false;
        }
        public IEnumerable<RaycastInfo> SphereCastAll(float3 origin, float radius, float3 direction, in float maxDistance = float.MaxValue)
        {
            var hits = Physics.SphereCastAll(origin, radius, direction, maxDistance);
            if (hits == null || hits.Length == 0) return Array.Empty<RaycastInfo>();

            return hits
                .Where(t => t.collider.GetComponent<RecycleableMonobehaviour>() != null)
                .Select(t =>
                {
                    var monoObj = t.collider.GetComponent<RecycleableMonobehaviour>();
                    return new RaycastInfo(monoObj.entity, true, t.distance, t.point);
                });
            //List<RaycastInfo> infos = new List<RaycastInfo>();
            //for (int i = 0; i < hits.Length; i++)
            //{
            //    var monoObj = hits[i].collider.GetComponent<RecycleableMonobehaviour>();
            //    if (monoObj == null) continue;

            //    var temp = new RaycastInfo(monoObj.entity, true, hits[i].distance, hits[i].point);
            //    infos.Add(temp);
            //}

            //return infos;
        }
        /// <summary>
        /// 레이캐스트를 실행합니다. TODO : 최적화
        /// </summary>
        /// <remarks>
        /// <see cref="TriggerBoundAttribute"/>가 있는 <see cref="Entity{T}"/>만 검출합니다. 
        /// 엔티티 이외에는 <seealso cref="Physics.Raycast(Ray)"/>를 사용하세요.
        /// </remarks>
        /// <param name="ray"></param>
        /// <param name="info"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        public bool Raycast(in Ray ray, out RaycastInfo info, in float maxDistance = float.MaxValue)
        {
            info = RaycastInfo.Empty;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                var monoObj = hit.collider.GetComponent<RecycleableMonobehaviour>();
                if (monoObj != null)
                {
                    info = new RaycastInfo(monoObj.entity, true, hit.distance, hit.point);

                    return true;
                }
            }
            return false;

            // TODO : Temp code
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
