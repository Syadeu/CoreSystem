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
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections.Threading;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    public sealed class WorldGridSystem : PresentationSystemEntity<WorldGridSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private WorldGrid m_Grid;
        private JobHandle m_GridUpdateJob;

        private NativeMultiHashMap<int, InstanceID> m_Indices;
        private NativeMultiHashMap<InstanceID, int> m_Entities;
        private NativeQueue<InstanceID>
            m_WaitForAdd, m_WaitForRemove;

        private UnsafeFixedQueue<InstanceID> m_NeedUpdateEntities;

        private EntityComponentSystem m_ComponentSystem;
        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_Grid = new WorldGrid(new AABB(0, 0), 2.5f);

            m_WaitForAdd = new NativeQueue<InstanceID>(AllocatorManager.Persistent);
            m_WaitForRemove = new NativeQueue<InstanceID>(AllocatorManager.Persistent);

            m_Indices = new NativeMultiHashMap<int, InstanceID>(1024, AllocatorManager.Persistent);
            m_Entities = new NativeMultiHashMap<InstanceID, int>(1024, AllocatorManager.Persistent);

            m_NeedUpdateEntities = new UnsafeFixedQueue<InstanceID>(128, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            RequestSystem<DefaultPresentationGroup, EntityComponentSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            m_ComponentSystem.OnComponentAdded -= M_ComponentSystem_OnComponentAdded;
            m_ComponentSystem.OnComponentRemove -= M_ComponentSystem_OnComponentRemove;

            m_EventSystem.RemoveEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }
        protected override void OnDispose()
        {
            m_WaitForAdd.Dispose();
            m_WaitForRemove.Dispose();

            m_Indices.Dispose();
            m_Entities.Dispose();

            m_NeedUpdateEntities.Dispose();

            m_ComponentSystem = null;
            m_EventSystem = null;
        }

        #region Binds

        private void Bind(EntityComponentSystem other)
        {
            m_ComponentSystem = other;

            m_ComponentSystem.OnComponentAdded += M_ComponentSystem_OnComponentAdded;
            m_ComponentSystem.OnComponentRemove += M_ComponentSystem_OnComponentRemove;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }

        private void M_ComponentSystem_OnComponentAdded(InstanceID arg1, Type arg2)
        {
            if (!TypeHelper.TypeOf<GridComponent>.Type.IsAssignableFrom(arg2)) return;

            m_GridUpdateJob.Complete();

            m_WaitForAdd.Enqueue(arg1);
        }
        private void M_ComponentSystem_OnComponentRemove(InstanceID arg1, Type arg2)
        {
            if (!TypeHelper.TypeOf<GridComponent>.Type.IsAssignableFrom(arg2)) return;

            m_GridUpdateJob.Complete();

            m_WaitForRemove.Enqueue(arg1);
        }

        #endregion

        #region Event Handlers

        private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        {
            if (ev.entity.HasComponent<GridComponent>())
            {
                m_NeedUpdateEntities.Enqueue(ev.entity.Idx);
            }

        }

        #endregion

        protected override PresentationResult BeforePresentation()
        {
            while (m_NeedUpdateEntities.TryDequeue(out InstanceID entity))
            {
                Remove(entity);
                Add(entity);
            }

            int removeCount = m_WaitForRemove.Count;
            for (int i = 0; i < removeCount; i++)
            {
                InstanceID entity = m_WaitForRemove.Dequeue();
                Remove(entity);
            }

            int addCount = m_WaitForAdd.Count;
            bool requireReindex = false;
            for (int i = 0; i < addCount; i++)
            {
                InstanceID entity = m_WaitForAdd.Dequeue();
                AABB aabb = entity.GetTransformWithoutCheck().aabb;

                requireReindex |= !m_Grid.Contains(aabb);
                if (requireReindex)
                {
                    AABB temp = m_Grid.aabb;
                    temp.Encapsulate(aabb);
                    m_Grid.aabb = temp;

                    m_WaitForAdd.Enqueue(entity);
                    $"require encapsulate for {entity}".ToLog();
                    continue;
                }

                var indices = m_Grid.AABBToIndices(aabb);
                for (int j = 0; j < indices.Length; j++)
                {
                    m_Indices.Add(indices[j].Index, entity);
                    m_Entities.Add(entity, indices[j].Index);
                }
            }

            if (requireReindex) FullIndexingUpdate();

            return base.BeforePresentation();
        }

        #endregion

        private void Add(in InstanceID entity)
        {
            AABB aabb = entity.GetTransformWithoutCheck().aabb;
            if (!m_Grid.Contains(aabb))
            {
                AABB temp = m_Grid.aabb;
                temp.Encapsulate(aabb);
                m_Grid.aabb = temp;

                $"require encapsulate for {entity}".ToLog();

                return;
            }

            var indices = m_Grid.AABBToIndices(aabb);
            for (int i = 0; i < indices.Length; i++)
            {
                m_Indices.Add(indices[i].Index, entity);
                m_Entities.Add(entity, indices[i].Index);
            }
        }
        private void Remove(in InstanceID entity)
        {
            if (!m_Entities.TryGetFirstValue(entity, out int index, out var iter)) return;

            do
            {
                m_Indices.Remove(index, entity);
            } while (m_Entities.TryGetNextValue(out index, ref iter));

            m_Entities.Remove(entity);
        }
        private bool HasEntityAt(in int3 location)
        {
            return m_Indices.ContainsKey(m_Grid.LocationToIndex(in location));
        }

        private unsafe void GetNearbyEntities(
            in int index, in int xzRange, in int yRange,
            ref UnsafeAllocator<InstanceID> output)
        {
            if (!output.IsCreated)
            {
                throw new Exception();
            }
            if (!m_Grid.Contains(in index))
            {
                throw new Exception();
            }

            int3
                location = m_Grid.IndexToLocation(index),
                start = new int3(location.x - xzRange, location.y - yRange, location.z - xzRange),
                end = new int3(location.x + xzRange, location.y + yRange, location.z + xzRange);

            int maxCount = (xzRange + 1) * (yRange + 1);
            int3* buffer = stackalloc int3[maxCount];
            for (int y = 0, i = 0; y < yRange + 1 && i < maxCount; y++)
            {
                for (int x = 0; x < xzRange && i < maxCount; x++)
                {
                    for (int z = 0; z < xzRange && i < maxCount; z++, i++)
                    {
                        buffer[i] = start + new int3(x, y, z);
                    }
                }
            }

            UnsafeBufferUtility.Sort(buffer, maxCount, new CloseDistanceComparer(location));

            for (int i = 0, added = 0;
                i < maxCount && added < output.Length;
                i++)
            {
                if (!HasEntityAt(buffer[i])) continue;

                m_Indices.TryGetFirstValue(m_Grid.LocationToIndex(buffer[i]), out InstanceID entity, out var iter);
                do
                {
                    output[added++] = entity;
                } while (added < output.Length && m_Indices.TryGetNextValue(out entity, ref iter));
            }
        }

        private struct CloseDistanceComparer : IComparer<int3>
        {
            private int3 from;

            public CloseDistanceComparer(int3 from)
            {
                this.from = from;
            }

            public int Compare(int3 x, int3 y)
            {
                int3
                    reletiveX = from - x,
                    reletiveY = from - y;

                int
                    sqrX = math.mul(reletiveX, reletiveX),
                    sqrY = math.mul(reletiveY, reletiveY);

                if (sqrX == sqrY) return 0;
                else if (sqrX < sqrY) return -1;
                else return 1;
            }
        }

        #region Jobs

        private void FullIndexingUpdate()
        {
            m_GridUpdateJob.Complete();
            
            m_WaitForRemove.Clear();

            int prevCount = m_Entities.Count();
            m_Entities.Dispose(); m_Indices.Dispose();
            m_Indices = new NativeMultiHashMap<int, InstanceID>(m_Grid.length, AllocatorManager.Persistent);

            int addCount = m_WaitForAdd.Count;
            m_Entities = new NativeMultiHashMap<InstanceID, int>(prevCount * 2 + addCount, AllocatorManager.Persistent);
            
            m_WaitForAdd.Clear();
            m_Indices.Clear();
            m_Entities.Clear();

            UpdateGridComponentJob componentJob = new UpdateGridComponentJob(
                m_Grid,
                ref m_Indices,
                ref m_Entities);

            var handle =
                ScheduleAt<UpdateGridComponentJob, GridComponent>(JobPosition.Before, componentJob);
            m_GridUpdateJob = JobHandle.CombineDependencies(m_GridUpdateJob, handle);

            "schedule full re indexing".ToLog();
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct UpdateGridComponentJob : IJobParallelForEntities<GridComponent>
        {
            [ReadOnly]
            private WorldGrid grid;
            [ReadOnly]
            private EntityTransformHashMap m_TrHashMap;

            [WriteOnly]
            private NativeMultiHashMap<int, InstanceID>.ParallelWriter indices;
            [ReadOnly]
            private NativeMultiHashMap<InstanceID, int>.ParallelWriter entities;

            public UpdateGridComponentJob(
                WorldGrid grid,
                ref NativeMultiHashMap<int, InstanceID> indices,
                ref NativeMultiHashMap<InstanceID, int> entities)
            {
                this.grid = grid;
                m_TrHashMap = EntityTransformStatic.GetHashMap();

                this.indices = indices.AsParallelWriter();
                this.entities = entities.AsParallelWriter();
            }

            public void Execute(in InstanceID entity, ref GridComponent component)
            {
                AABB aabb = m_TrHashMap.GetTransform(entity).aabb;

                component.m_Indices = grid.AABBToIndices(aabb);
                for (int i = 0; i < component.m_Indices.Length; i++)
                {
                    indices.Add(component.m_Indices[i].Index, entity);
                    entities.Add(entity, component.m_Indices[i].Index);
                }
            }
        }

        #endregion

        public void InitializeGrid(in AABB aabb, in float cellSize)
        {
            m_Grid.aabb = aabb;
            m_Grid.cellSize = cellSize;

            FullIndexingUpdate();
        }

        public void UpdateEntity(in InstanceID entity)
        {
            m_GridUpdateJob.Complete();

            Remove(entity);
            Add(entity);
        }
    }

    public struct GridComponent : IEntityComponent
    {
        internal FixedList128Bytes<GridIndex> m_Indices;

    }
}
