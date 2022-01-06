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
    public sealed class WorldGridSystem : PresentationSystemEntity<WorldGridSystem>,
        INotifySystemModule<WorldGridGLModule>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private WorldGrid m_Grid;
        private JobHandle m_GridUpdateJob;

        internal NativeMultiHashMap<ulong, InstanceID> m_Indices;
        private NativeMultiHashMap<InstanceID, ulong> m_Entities;
        private NativeQueue<InstanceID>
            m_WaitForAdd, m_WaitForRemove;

        private UnsafeFixedQueue<InstanceID> m_NeedUpdateEntities;

        private EntityComponentSystem m_ComponentSystem;
        private EventSystem m_EventSystem;

        internal WorldGrid Grid => m_Grid;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_Grid = new WorldGrid(new AABB(0, 0), 2.5f);

            m_WaitForAdd = new NativeQueue<InstanceID>(AllocatorManager.Persistent);
            m_WaitForRemove = new NativeQueue<InstanceID>(AllocatorManager.Persistent);

            m_Indices = new NativeMultiHashMap<ulong, InstanceID>(1024, AllocatorManager.Persistent);
            m_Entities = new NativeMultiHashMap<InstanceID, ulong>(1024, AllocatorManager.Persistent);

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
            bool requireReindex = false;

            while (m_NeedUpdateEntities.TryDequeue(out InstanceID entity))
            {
                Remove(entity);
                requireReindex |= !Add(entity);
            }

            if (!requireReindex)
            {
                int removeCount = m_WaitForRemove.Count;
                for (int i = 0; i < removeCount; i++)
                {
                    InstanceID entity = m_WaitForRemove.Dequeue();
                    Remove(entity);
                }

                int addCount = m_WaitForAdd.Count;

                for (int i = 0; i < addCount; i++)
                {
                    InstanceID entity = m_WaitForAdd.Dequeue();
                    AABB aabb = entity.GetTransformWithoutCheck().aabb;

                    requireReindex |= !m_Grid.Contains(aabb);
                    if (requireReindex)
                    {
                        Increase(aabb);

                        m_WaitForAdd.Enqueue(entity);
                        //$"require encapsulate for {entity}".ToLog();
                        continue;
                    }

                    var indices = m_Grid.AABBToIndices(aabb);
                    for (int j = 0; j < indices.Length; j++)
                    {
                        m_Indices.Add(indices[j].Index, entity);
                        m_Entities.Add(entity, indices[j].Index);
                    }

                    //ulong index = m_Grid.PositionToIndex(entity.GetTransformWithoutCheck().position);
                    //m_Indices.Add(index, entity);
                    //m_Entities.Add(entity, index);
                }
            }
            
            if (requireReindex) FullIndexingUpdate();

            return base.BeforePresentation();
        }

        #endregion

        private void Increase(AABB aabb)
        {
            AABB temp = m_Grid.aabb;

            float3
                min = math.round(aabb.min),
                max = math.round(aabb.max),

                restMin = min % m_Grid.cellSize,
                restMax = max % m_Grid.cellSize,

                targetMin = min + restMin - m_Grid.cellSize,
                targetMax = max - restMax + m_Grid.cellSize;

            temp.Encapsulate(targetMin);
            temp.Encapsulate(targetMax);
            m_Grid.aabb = temp;
        }
        private bool Add(in InstanceID entity)
        {
            AABB aabb = entity.GetTransformWithoutCheck().aabb;
            if (!m_Grid.Contains(aabb))
            {
                Increase(aabb);

                //$"require encapsulate for {entity}".ToLog();
                return false;
            }

            var indices = m_Grid.AABBToIndices(aabb);
            for (int i = 0; i < indices.Length; i++)
            {
                m_Indices.Add(indices[i].Index, entity);
                m_Entities.Add(entity, indices[i].Index);
            }

            //ulong index = m_Grid.PositionToIndex(entity.GetTransformWithoutCheck().position);
            //m_Indices.Add(index, entity);
            //m_Entities.Add(entity, index);

            return true;
        }
        private void Remove(in InstanceID entity)
        {
            if (!m_Entities.TryGetFirstValue(entity, out ulong index, out var iter)) return;

            do
            {
                if (m_Indices.CountValuesForKey(index) == 1)
                {
                    m_Indices.Remove(index);
                }
                else m_Indices.Remove(index, entity);

            } while (m_Entities.TryGetNextValue(out index, ref iter));

            m_Entities.Remove(entity);
        }
        
        private unsafe void GetNearbyEntities(
            in ulong index, in int xzRange, in int yRange,
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

        internal void CompleteJobs()
        {
            m_GridUpdateJob.Complete();
        }
        private void FullIndexingUpdate()
        {
            m_GridUpdateJob.Complete();
            
            m_WaitForRemove.Clear();
            m_WaitForAdd.Clear();

            int prevCount = m_Entities.Capacity;
            int targetCap = prevCount - m_Entities.Count() < prevCount / 3 ? prevCount * 2 : prevCount;

            if (prevCount != targetCap)
            {
                m_Entities.Dispose(); m_Indices.Dispose();
                m_Indices = new NativeMultiHashMap<ulong, InstanceID>(targetCap, AllocatorManager.Persistent);
                m_Entities = new NativeMultiHashMap<InstanceID, ulong>(targetCap, AllocatorManager.Persistent);
            }
            else
            {
                m_Indices.Clear();
                m_Entities.Clear();
            }

            UpdateGridComponentJob componentJob = new UpdateGridComponentJob(
                m_Grid,
                ref m_Indices,
                ref m_Entities);

            var handle =
                ScheduleAt<UpdateGridComponentJob, GridComponent>(JobPosition.Before, componentJob);
            m_GridUpdateJob = JobHandle.CombineDependencies(m_GridUpdateJob, handle);

            //"schedule full re indexing".ToLog();
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct UpdateGridComponentJob : IJobParallelForEntities<GridComponent>
        {
            [ReadOnly]
            private WorldGrid grid;
            [ReadOnly]
            private EntityTransformHashMap m_TrHashMap;

            [WriteOnly]
            private NativeMultiHashMap<ulong, InstanceID>.ParallelWriter indices;
            [WriteOnly]
            private NativeMultiHashMap<InstanceID, ulong>.ParallelWriter entities;

            public UpdateGridComponentJob(
                WorldGrid grid,
                ref NativeMultiHashMap<ulong, InstanceID> indices,
                ref NativeMultiHashMap<InstanceID, ulong> entities)
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

                //ulong index = grid.PositionToIndex(m_TrHashMap.GetTransform(entity).position);
                //indices.Add(index, entity);
                //entities.Add(entity, index);
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
        public bool HasEntityAt(in int3 location)
        {
            return m_Indices.ContainsKey(m_Grid.LocationToIndex(in location));
        }
    }

    public struct GridComponent : IEntityComponent
    {
        internal FixedList512Bytes<GridIndex> m_Indices;
    }
}
