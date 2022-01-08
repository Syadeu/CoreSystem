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
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    public sealed class WorldGridSystem : PresentationSystemEntity<WorldGridSystem>
        , INotifySystemModule<WorldGridShapesModule>,
        INotifySystemModule<WorldGridPathModule>,
        INotifySystemModule<GridDetectorModule>
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
        public float CellSize => m_Grid.cellSize;

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
                requireReindex |= !Add(entity, out bool locationChanged);

                if (locationChanged)
                {
                    m_EventSystem.PostEvent(OnGridLocationChangedEvent.GetEvent(entity));
                }
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
                    requireReindex |= !Add(entity, out bool locationChanged);

                    if (locationChanged)
                    {
                        m_EventSystem.PostEvent(OnGridLocationChangedEvent.GetEvent(entity));
                    }
                }
            }
            
            if (requireReindex) FullIndexingUpdate();

            return base.BeforePresentation();
        }

        #endregion

        private void Increase(AABB aabb)
        {
            float3
                min = math.round(aabb.min),
                max = math.round(aabb.max),

                restMin = min % m_Grid.cellSize,
                restMax = max % m_Grid.cellSize,

                targetMin = min + restMin - m_Grid.cellSize,
                targetMax = max - restMax + m_Grid.cellSize;

            AABB temp;
            if (m_Grid.aabb.IsZero())
            {
                temp = new AABB(targetMin, 0);
                temp.Encapsulate(targetMax);
            }
            else
            {
                temp = m_Grid.aabb;

                temp.Encapsulate(targetMin);
                temp.Encapsulate(targetMax);
            }
            
            m_Grid.aabb = temp;
        }
        private bool Add(in InstanceID entity, out bool locationChanged)
        {
            AABB aabb = entity.GetTransformWithoutCheck().aabb;
            locationChanged = false;
            if (!m_Grid.Contains(aabb))
            {
                Increase(aabb);

                //$"require encapsulate for {entity}".ToLog();
                return false;
            }

            ref GridComponent component = ref entity.GetComponent<GridComponent>();
            if (component.FixedSize.Equals(0))
            {
                var indices = m_Grid.AABBToIndices(aabb);
                if (indices.Length == 0)
                {
                    $"no index found to {entity}".ToLog();
                }

                if (component.m_Indices.Length != indices.Length)
                {
                    locationChanged = true;

                    for (int i = 0; i < indices.Length; i++)
                    {
                        m_Indices.Add(indices[i].Index, entity);
                        m_Entities.Add(entity, indices[i].Index);
                    }
                }
                else
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        m_Indices.Add(indices[i].Index, entity);
                        m_Entities.Add(entity, indices[i].Index);

                        locationChanged |= component.m_Indices[i].Index != indices[i].Index;
                    }
                }
                
                component.m_Indices = indices;
            }
            else
            {
                float3 targetPosition = aabb.lowerCenter;
                if (component.SizeAlignment != Alignment.Center)
                {
                    if ((component.SizeAlignment & Alignment.Left) == Alignment.Left)
                    {
                        targetPosition.x -= aabb.extents.x;
                    }
                    if ((component.SizeAlignment & Alignment.Right) == Alignment.Right)
                    {
                        targetPosition.x += aabb.extents.x;
                    }
                    if ((component.SizeAlignment & Alignment.Down) == Alignment.Down)
                    {
                        targetPosition.z -= aabb.extents.z;
                    }
                    if ((component.SizeAlignment & Alignment.Up) == Alignment.Up)
                    {
                        targetPosition.z += aabb.extents.z;
                    }
                }
                int3 location = m_Grid.PositionToLocation(targetPosition);

                var indices = new FixedList512Bytes<GridIndex>();

                for (int y = 0; y < component.FixedSize.y; y++)
                {
                    for (int x = 0; x < component.FixedSize.x; x++)
                    {
                        for (int z = 0; z < component.FixedSize.z; z++)
                        {
                            ulong index = m_Grid.LocationToIndex(location + new int3(x, y, z));

                            m_Indices.Add(index, entity);
                            m_Entities.Add(entity, index);

                            indices.Add(new GridIndex(m_Grid, index));
                        }
                    }
                }

                if (component.m_Indices.Length != indices.Length)
                {
                    locationChanged = true;
                }
                else
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        locationChanged |= component.m_Indices[i].Index != indices[i].Index;
                    }
                }

                component.m_Indices = indices;
            }

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
            PostChangedEventJob postChangedEventJob = new PostChangedEventJob(
                ref m_Entities,
                m_EventSystem
                );

            var handle =
                ScheduleAt<UpdateGridComponentJob, GridComponent>(JobPosition.Before, componentJob);
            m_GridUpdateJob = JobHandle.CombineDependencies(m_GridUpdateJob, handle);

            var handle2 =
                ScheduleAt(JobPosition.Before, postChangedEventJob);
            m_GridUpdateJob = JobHandle.CombineDependencies(m_GridUpdateJob, handle2);

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

                if (component.FixedSize.Equals(0))
                {
                    component.m_Indices = grid.AABBToIndices(aabb);
                    for (int i = 0; i < component.m_Indices.Length; i++)
                    {
                        indices.Add(component.m_Indices[i].Index, entity);
                        entities.Add(entity, component.m_Indices[i].Index);
                    }

                    return;
                }

                float3 targetPosition = aabb.lowerCenter;
                if (component.SizeAlignment != Alignment.Center)
                { 
                    if ((component.SizeAlignment & Alignment.Left) == Alignment.Left)
                    {
                        targetPosition.x -= aabb.extents.x;
                    }
                    if ((component.SizeAlignment & Alignment.Right) == Alignment.Right)
                    {
                        targetPosition.x += aabb.extents.x;
                    }
                    if ((component.SizeAlignment & Alignment.Down) == Alignment.Down)
                    {
                        targetPosition.z -= aabb.extents.z;
                    }
                    if ((component.SizeAlignment & Alignment.Up) == Alignment.Up)
                    {
                        targetPosition.z += aabb.extents.z;
                    }
                }
                int3 location = grid.PositionToLocation(targetPosition);

                for (int y = 0; y < component.FixedSize.y; y++)
                {
                    for (int x = 0; x < component.FixedSize.x; x++)
                    {
                        for (int z = 0; z < component.FixedSize.z; z++)
                        {
                            ulong index = grid.LocationToIndex(location + new int3(x, y, z));

                            indices.Add(index, entity);
                            entities.Add(entity, index);
                        }
                    }
                }
            }
        }
        private struct PostChangedEventJob : IJob
        {
            private NativeMultiHashMap<InstanceID, ulong> entities;
            private PresentationSystemID<EventSystem> m_EventSystemID;

            public PostChangedEventJob(
                ref NativeMultiHashMap<InstanceID, ulong> entities, EventSystem eventSystem)
            {
                this.entities = entities;
                m_EventSystemID = eventSystem.SystemID;
            }

            public void Execute()
            {
                EventSystem eventSystem = m_EventSystemID.System;
                var array = entities.GetKeyArray(AllocatorManager.Temp);

                for (int i = 0; i < array.Length; i++)
                {
                    eventSystem.PostEvent(OnGridLocationChangedEvent.GetEvent(array[i]));
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
            Add(entity, out bool changed);

            if (changed)
            {
                m_EventSystem.PostEvent(OnGridLocationChangedEvent.GetEvent(entity));
            }
        }
        public bool HasEntityAt(in GridIndex index)
        {
            return m_Indices.ContainsKey(index.Index);
        }
        public bool TryGetEntitiesAt(in GridIndex index, out EntityEnumerator iter)
        {
            if (!m_Indices.ContainsKey(index.Index))
            {
                iter = default(EntityEnumerator);
                return false;
            }

            iter = new EntityEnumerator(m_Indices.GetValuesForKey(index.Index));
            return true;
        }

        [BurstCompatible]
        public struct EntityEnumerator : IEnumerator<InstanceID>
        {
            private NativeMultiHashMap<ulong, InstanceID>.Enumerator m_Iterator;

            public EntityEnumerator(NativeMultiHashMap<ulong, InstanceID>.Enumerator iter)
            {
                m_Iterator = iter;
            }

            public InstanceID Current => m_Iterator.Current;
            [NotBurstCompatible]
            object IEnumerator.Current => m_Iterator.Current;

            public void Dispose()
            {
                m_Iterator.Dispose();
            }

            public bool MoveNext()
            {
                return m_Iterator.MoveNext();
            }
            public void Reset()
            {
                m_Iterator.Reset();
            }
        }

        public bool ValidateIndex(in GridIndex index)
        {
            if (!m_Grid.m_CheckSum.Equals(index.m_CheckSum) || !m_Grid.Contains(index.Index))
            {
                return false;
            }
            return true;
        }

        #region Get Range

        [SkipLocalsInit]
        public void GetRange(in GridIndex from,
            in int3 range,
            ref NativeList<GridIndex> output)
        {
            if (!ValidateIndex(in from))
            {
                "err".ToLogError();
                return;
            }

            int maxCount = (range.x + 1) * (range.z + 1) * (range.y + 1);
            if (maxCount > 255)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"You\'re trying to get range of grid that exceeding length 255. " +
                    $"Buffer is fixed to 255 length, overloading indices({maxCount - 255}) will be dropped.");
            }

            output.Clear();

            int3
                location = from.Location,
                minRange, maxRange;
            m_Grid.GetMinMaxLocation(out minRange, out maxRange);

            int
                minX = location.x - range.x < 0 ? 0 : math.min(location.x - range.x, maxRange.x),
                maxX = location.x + range.x < 0 ? 0 : math.min(location.x + range.x, maxRange.x),

                minY = location.y - range.y < 0 ?
                    math.max(location.y - range.y, minRange.y) : math.min(location.y - range.y, maxRange.y),
                maxY = location.y + range.y < 0 ?
                    math.max(location.y + range.y, minRange.y) : math.min(location.y + range.y, maxRange.y),

                minZ = location.z - range.z < 0 ? 0 : math.min(location.z - range.z, maxRange.z),
                maxZ = location.z + range.z < 0 ? 0 : math.min(location.z + range.z, maxRange.z);

            int3
                start = new int3(minX, minY, minZ),
                end = new int3(maxX, maxY, maxZ),

                dir = end - start;

            unsafe
            {
                int3* buffer = stackalloc int3[maxCount];
                int count = 0;
                for (int y = start.y; y < end.y + 1 && count < maxCount; y++)
                {
                    for (int x = start.x; x < end.x + 1 && count < maxCount; x++)
                    {
                        for (int z = start.z;
                            z < end.z + 1 && count < maxCount;
                            z++, count++)
                        {
                            buffer[count] = new int3(x, y, z);
                        }
                    }
                }

                UnsafeBufferUtility.Sort(buffer, count, new CloseDistanceComparer(location));

                for (int i = 0; i < count && i < 255; i++)
                {
                    output.Add(new GridIndex(m_Grid.m_CheckSum, buffer[i]));
                }
            }
        }
        public void GetRange(in InstanceID from,
            in int3 range,
            ref FixedList4096Bytes<GridIndex> output)
        {
            CompleteJobs();

            if (!m_Entities.TryGetFirstValue(from, out ulong index, out var iter))
            {
                "err".ToLogError();
                return;
            }

            // TODO : Temp code
            GetRange(new GridIndex(m_Grid.m_CheckSum, index), range, ref output);
        }
        [SkipLocalsInit]
        public void GetRange(in GridIndex from,
            in int3 range,
            ref FixedList4096Bytes<GridIndex> output)
        {
            if (!ValidateIndex(in from))
            {
                "err".ToLogError();
                return;
            }

            int maxCount = (range.x + 1) * (range.z + 1) * (range.y + 1);
            if (maxCount > 255)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"You\'re trying to get range of grid that exceeding length 255. " +
                    $"Buffer is fixed to 255 length, overloading indices({maxCount - 255}) will be dropped.");
            }

            output.Clear();

            int3
                location = from.Location,
                minRange, maxRange;
            m_Grid.GetMinMaxLocation(out minRange, out maxRange);

            int
                minX = location.x - range.x < 0 ? 0 : math.min(location.x - range.x, maxRange.x),
                maxX = location.x + range.x < 0 ? 0 : math.min(location.x + range.x, maxRange.x),

                minY = location.y - range.y < 0 ?
                    math.max(location.y - range.y, minRange.y) : math.min(location.y - range.y, maxRange.y),
                maxY = location.y + range.y < 0 ?
                    math.max(location.y + range.y, minRange.y) : math.min(location.y + range.y, maxRange.y),

                minZ = location.z - range.z < 0 ? 0 : math.min(location.z - range.z, maxRange.z),
                maxZ = location.z + range.z < 0 ? 0 : math.min(location.z + range.z, maxRange.z);
            //if (log) $"minmax: {minRange}, {maxRange}".ToLog();
            int3
                start = new int3(minX, minY, minZ),
                end = new int3(maxX, maxY, maxZ),
                
                dir = end - start;

            
            unsafe
            {
                int3* buffer = stackalloc int3[maxCount];
                int count = 0;
                for (int y = start.y; y < end.y + 1 && count < maxCount; y++)
                {
                    for (int x = start.x; x < end.x + 1 && count < maxCount; x++)
                    {
                        for (int z = start.z;
                            z < end.z + 1 && count < maxCount;
                            z++, count++)
                        {
                            buffer[count] = new int3(x, y, z);
                        }
                    }
                }
                //if (log) $"{count} < {maxCount}: {start}, {end}".ToLog();
                UnsafeBufferUtility.Sort(buffer, count, new CloseDistanceComparer(location));

                for (int i = 0; i < count && i < 255; i++)
                {
                    output.Add(new GridIndex(m_Grid.m_CheckSum, buffer[i]));
                }
            }
        }
        //[SkipLocalsInit]
        //public void GetEntitiesInRange(in GridIndex from,
        //    in int xzRange, in int yRange,
        //    ref NativeList<InstanceID> output)
        //{
        //    if (!ValidateIndex(in index))
        //    {
        //        throw new Exception();
        //    }

        //    output.Clear();

        //    int3
        //        location = from.Location,
        //        maxRange = m_Grid.gridSize,
        //        minRange = new int3(0, -maxRange.y, 0);
        //    int
        //        minY = location.y - range.y < 0 ?
        //            math.max(location.y - range.y, minRange.y) : math.min(location.y - range.y, maxRange.y),
        //        maxY = location.y + range.y < 0 ?
        //            math.max(location.y + range.y, minRange.y) : math.min(location.y + range.y, maxRange.y);

        //    int3
        //        start = new int3(math.min(location.x - range.x, maxRange.x), minY, math.min(location.z - range.z, maxRange.z)),
        //        end = new int3(math.min(location.x + range.x, maxRange.x), maxY, math.min(location.z + range.z, maxRange.z));

        //    int maxCount = (range.x + 1) * (range.z + 1) * (range.y + 1);
        //    unsafe
        //    {
        //        int3* buffer = stackalloc int3[maxCount];
        //        for (int y = start.y, i = 0; y < end.y + 1 && i < maxCount; y++)
        //        {
        //            for (int x = start.x; x < end.x + 1 && i < maxCount; x++)
        //            {
        //                for (int z = start.x;
        //                    z < end.z + 1 && i < maxCount;
        //                    z++, i++)
        //                {
        //                    buffer[i] = new int3(x, y, z);
        //                }
        //            }
        //        }

        //        UnsafeBufferUtility.Sort(buffer, maxCount, new CloseDistanceComparer(location));

        //        for (int i = 0;
        //            i < maxCount;
        //            i++)
        //        {
        //            if (!m_Indices.TryGetFirstValue(m_Grid.LocationToIndex(buffer[i]), out InstanceID entity, out var iter))
        //            {
        //                continue;
        //            }

        //            do
        //            {
        //                output.Add(entity);
        //            } while (m_Indices.TryGetNextValue(out entity, ref iter));
        //        }
        //    }
        //}

        #endregion

        #region Index

        public bool HasDirection(in GridIndex index, in Direction direction)
        {
            int3 location = CalculateDirection(in index, in direction);

            return m_Grid.Contains(location);
        }
        public bool TryGetDirection(in GridIndex index, in Direction direction, out GridIndex result)
        {
            int3 location = CalculateDirection(in index, in direction);

            if (!m_Grid.Contains(location))
            {
                result = default(GridIndex);
                return false;
            }

            result = new GridIndex(m_Grid.m_CheckSum, location);
            return true;
        }
        private static int3 CalculateDirection(in GridIndex index, in Direction direction)
        {
            int3 location = index.Location;
            if ((direction & Direction.Up) == Direction.Up)
            {
                location.y += 1;
            }
            if ((direction & Direction.Down) == Direction.Down)
            {
                location.y -= 1;
            }
            if ((direction & Direction.Left) == Direction.Left)
            {
                location.x -= 1;
            }
            if ((direction & Direction.Right) == Direction.Right)
            {
                location.x += 1;
            }
            if ((direction & Direction.Forward) == Direction.Forward)
            {
                location.z += 1;
            }
            if ((direction & Direction.Forward) == Direction.Backward)
            {
                location.z -= 1;
            }
            return location;
        }

        public float3 IndexToPosition(in GridIndex index)
        {
            return m_Grid.IndexToPosition(index.Index);
        }

        #endregion

        public bool HasPath(
            in GridIndex from,
            in GridIndex to,
            out int pathFound,
            in int maxIteration = 32
            )
            => GetModule<WorldGridPathModule>().HasPath(in from, in to, out pathFound, in maxIteration);
        public bool GetPath(
            in GridIndex from,
            in GridIndex to,
            ref FixedList4096Bytes<GridIndex> foundPath,
            in int maxIteration = 32
            )
            => GetModule<WorldGridPathModule>().GetPath(in from, in to, ref foundPath, in maxIteration);
        // TODO : Temp code
        public bool GetPath(
            in InstanceID from,
            in GridIndex to,
            ref FixedList4096Bytes<GridIndex> foundPath,
            in int maxIteration = 32
            )
        {
            if (!m_Entities.TryGetFirstValue(from, out ulong index, out var iter)) return false;

            return GetModule<WorldGridPathModule>().GetPath(new GridIndex(m_Grid.m_CheckSum, index), in to, ref foundPath, in maxIteration);
        }
    }

    internal sealed class WorldGridPathModule : PresentationSystemModule<WorldGridSystem>
    {
        private struct PathTile : IEmpty
        {
            public GridIndex parent, index;
            public Direction direction;
            public uint parentArrayIdx, arrayIdx;

            // TODO : 더 작은걸로 6
            public ClosedBoolen6 closed;

            public PathTile(GridIndex index)
            {
                this = default(PathTile);

                this.index = index;
            }
            public PathTile(PathTile parent, GridIndex index, Direction direction, uint arrayIdx)
            {
                this = default(PathTile);

                this.parent = parent.index;
                this.index = index;
                this.direction = direction;
                parentArrayIdx = parent.arrayIdx;
                this.arrayIdx = arrayIdx;
            }

            public void SetClose(in Direction direction, bool value)
            {
                if ((direction & Direction.Up) == Direction.Up)
                {
                    closed[0] = value;
                }
                if ((direction & Direction.Down) == Direction.Down)
                {
                    closed[1] = value;
                }
                if ((direction & Direction.Left) == Direction.Left)
                {
                    closed[2] = value;
                }
                if ((direction & Direction.Right) == Direction.Right)
                {
                    closed[3] = value;
                }
                if ((direction & Direction.Forward) == Direction.Forward)
                {
                    closed[4] = value;
                }
                if ((direction & Direction.Backward) == Direction.Backward)
                {
                    closed[5] = value;
                }
            }
            public bool IsRoot() => parent.IsEmpty();

            public bool IsEmpty()
            {
                return parent.IsEmpty() && index.IsEmpty();
            }
        }
        private struct ClosedBoolen6
        {
            private bool
                a0, a1, a2,
                b0, b1, b2;

            public bool this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => a0,
                        1 => a1,
                        2 => a2,
                        3 => b0,
                        4 => b1,
                        5 => b2,
                        _ => throw new IndexOutOfRangeException()
                    };
                }
                set
                {
                    if (index == 0) a0 = value;
                    else if (index == 1) a1 = value;
                    else if (index == 2) a2 = value;
                    else if (index == 3) b0 = value;
                    else if (index == 4) b1 = value;
                    else if (index == 5) b2 = value;
                }
            }
        }

        [SkipLocalsInit]
        public bool HasPath(
            in GridIndex from,
            in GridIndex to,
            out int pathFound,
            in int maxIteration = 32
            )
        {
            PathTile root = new PathTile(from);
            CalculatePathTile(ref root);
            unsafe
            {
                GridIndex* four = stackalloc GridIndex[6];
                for (int i = 0; i < 6; i++)
                {
                    System.TryGetDirection(from, (Direction)(1 << i), out GridIndex result);
                    four[i] = result;
                }

                PathTile* path = stackalloc PathTile[512];
                path[0] = root;

                pathFound = 1;
                uint count = 1, iteration = 0, currentTileIdx = 0;

                while (
                    iteration < maxIteration &&
                    count < 512 &&
                    path[count - 1].index.Index != to.Index)
                {
                    ref PathTile lastTileData = ref path[currentTileIdx];

                    Direction nextDirection = GetLowestCost(ref lastTileData, in to, out GridIndex result);
                    if (nextDirection < 0)
                    {
                        pathFound--;

                        if (pathFound <= 0) break;

                        ref PathTile parentTile = ref path[lastTileData.parentArrayIdx];
                        parentTile.SetClose(lastTileData.direction, true);

                        currentTileIdx = lastTileData.parentArrayIdx;

                        iteration++;
                        continue;
                    }

                    PathTile nextTile = GetOrCreateNext(path, count, lastTileData, result, nextDirection, out bool isNew);

                    lastTileData.SetClose(nextDirection, true);
                    CalculatePathTile(ref nextTile);

                    if (isNew)
                    {
                        path[count] = (nextTile);
                        currentTileIdx = count;
                        count++;
                    }
                    else
                    {
                        currentTileIdx = nextTile.arrayIdx;
                    }

                    pathFound++;
                }

                // Path Found
                if (path[count - 1].index.Equals(to))
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (UnsafeBufferUtility.Contains(four, 6, path[i].index))   
                        {
                            path[i].parent = from;
                            path[i].parentArrayIdx = 0;
                        }
                    }

                    int sortedFound = 0;
                    PathTile current = path[count - 1];
                    for (int i = 0; i < pathFound && !current.index.Equals(from); i++, sortedFound++)
                    {
                        current = path[current.parentArrayIdx];
                    }

                    pathFound = sortedFound;

                    return true;
                }
            }

            return false;
        }
        [SkipLocalsInit]
        public bool GetPath(
            in GridIndex from,
            in GridIndex to,
            ref FixedList4096Bytes<GridIndex> foundPath,
            in int maxIteration = 32
            )
        {
            PathTile root = new PathTile(from);
            CalculatePathTile(ref root);
            unsafe
            {
                GridIndex* four = stackalloc GridIndex[6];
                for (int i = 0; i < 6; i++)
                {
                    System.TryGetDirection(from, (Direction)(1 << i), out GridIndex result);
                    four[i] = result;
                }

                PathTile* path = stackalloc PathTile[512];
                path[0] = root;

                int pathFound = 1;
                uint count = 1, iteration = 0, currentTileIdx = 0;

                while (
                    iteration < maxIteration &&
                    count < 512 &&
                    path[count - 1].index.Index != to.Index)
                {
                    ref PathTile lastTileData = ref path[currentTileIdx];

                    Direction nextDirection = GetLowestCost(ref lastTileData, in to, out GridIndex result);
                    if (nextDirection == Direction.NONE)
                    {
                        pathFound--;

                        if (pathFound <= 0) break;

                        ref PathTile parentTile = ref path[lastTileData.parentArrayIdx];
                        parentTile.SetClose(lastTileData.direction, true);

                        currentTileIdx = lastTileData.parentArrayIdx;

                        iteration++;
                        continue;
                    }

                    PathTile nextTile = GetOrCreateNext(path, count, lastTileData, result, nextDirection, out bool isNew);

                    lastTileData.SetClose(nextDirection, true);
                    CalculatePathTile(ref nextTile);

                    if (isNew)
                    {
                        path[count] = (nextTile);
                        currentTileIdx = count;
                        count++;
                    }
                    else
                    {
                        currentTileIdx = nextTile.arrayIdx;
                    }

                    pathFound++;
                }

                // Path Found
                if (path[count - 1].index.Equals(to))
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (UnsafeBufferUtility.Contains(four, 6, path[i].index))
                        {
                            path[i].parent = from;
                            path[i].parentArrayIdx = 0;
                        }
                    }

                    GridIndex* arr = stackalloc GridIndex[pathFound];

                    int length = 0;
                    PathTile current = path[count - 1];
                    for (int i = 0; i < pathFound && !current.index.Equals(from); i++, length++)
                    {
                        arr[i] = current.index;

                        current = path[current.parentArrayIdx];
                    }

                    foundPath.Add(from);
                    for (int i = length - 1; i >= 0; i--)
                    {
                        foundPath.Add(arr[i]);
                    }

                    return true;
                }
            }

            return false;
        }

        private unsafe PathTile GetOrCreateNext(PathTile* array, in uint length,
            in PathTile from, in GridIndex target, in Direction targetDirection, out bool isNew)
        {
            for (int i = 0; i < length; i++)
            {
                if (array[i].index.Equals(target))
                {
                    isNew = false;
                    return array[i];
                }
            }

            isNew = true;
            return new PathTile(from, target, targetDirection, length);
        }
        private Direction GetLowestCost(ref PathTile prev, in GridIndex to, out GridIndex result)
        {
            Direction lowest = 0;
            result = default(GridIndex);
            int cost = int.MaxValue;

            for (int i = 0; i < 6; i++)
            {
                if (prev.closed[i]) continue;

                if (!System.TryGetDirection(prev.index, (Direction)(1 << i), out GridIndex index))
                {
                    prev.closed[i] = true;
                    continue;
                }

                int3 dir = index.Location - to.Location;
                int tempCost = (dir.x * dir.x) + (dir.y * dir.y) + (dir.z * dir.z);

                if (tempCost < cost)
                {
                    lowest = (Direction)(1 << i);
                    result = index;
                    cost = tempCost;
                }
            }

            return lowest;
        }
        private void CalculatePathTile(ref PathTile tile)
        {
            for (int i = 0; i < 6; i++)
            {
                if (!System.TryGetDirection(tile.index, (Direction)(1 << i), out GridIndex target))
                {
                    tile.closed[i] = false;
                    continue;
                }
                
                if (System.TryGetEntitiesAt(in target, out var iter))
                {
                    bool isBlock = false;
                    using (iter)
                    {
                        while (iter.MoveNext())
                        {
                            GridComponent gridComponent = iter.Current.GetComponent<GridComponent>();
                            isBlock |= gridComponent.ObstacleType == GridComponent.Obstacle.Block;
                        }
                    }

                    tile.closed[i] = isBlock;
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 위치가 바뀐 경우 <seealso cref="OnGridLocationChangedEvent"/> 이벤트를 발생시킵니다.
    /// </remarks>
    public struct GridComponent : IEntityComponent
    {
        public enum Obstacle
        {
            None,

            Block
        }

        internal FixedList512Bytes<GridIndex> m_Indices;
        private int3 m_FixedSize;
        private Alignment m_SizeAlignment;
        private Obstacle m_ObstacleType;

        public FixedList512Bytes<GridIndex> Indices => m_Indices;
        public int3 FixedSize { get => m_FixedSize; set => m_FixedSize = value; }
        public Alignment SizeAlignment { get => m_SizeAlignment; set => m_SizeAlignment = value; }
        public Obstacle ObstacleType { get => m_ObstacleType; set => m_ObstacleType = value; }

        public bool IsMyIndex(GridIndex index)
        {
            for (int i = 0; i < m_Indices.Length; i++)
            {
                if (m_Indices[i].Equals(index)) return true;
            }
            return false;
        }
    }

    internal unsafe sealed class GridDetectorModule : PresentationSystemModule<WorldGridSystem>
    {
        /// <summary>
        /// Grid 를 감시하는 Observer 들의 그리드 인덱스들을 저장합니다.
        /// </summary>
        private UnsafeMultiHashMap<GridIndex, InstanceID> m_GridObservers;
        // 1. targeted, 2. spotteds(observers)
        private UnsafeMultiHashMap<InstanceID, InstanceID> m_TargetedEntities;

        private Unity.Profiling.ProfilerMarker
            m_UpdateGridDetectionMarker = new Unity.Profiling.ProfilerMarker($"{nameof(GridDetectorModule)}.UpdateGridDetection"),
            m_UpdateDetectPositionMarker = new Unity.Profiling.ProfilerMarker($"{nameof(GridDetectorModule)}.UpdateDetectPosition");

        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            m_GridObservers = new UnsafeMultiHashMap<GridIndex, InstanceID>(1024, AllocatorManager.Persistent);
            m_TargetedEntities = new UnsafeMultiHashMap<InstanceID, InstanceID>(1024, AllocatorManager.Persistent);

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
        }
        protected override void OnShutDown()
        {
            m_EventSystem.RemoveEvent<OnGridLocationChangedEvent>(OnGridLocationChangedEventHandler);
        }
        protected override void OnDispose()
        {
            m_GridObservers.Dispose();
            m_TargetedEntities.Dispose();

            m_EventSystem = null;
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnGridLocationChangedEvent>(OnGridLocationChangedEventHandler);
        }

        private void OnGridLocationChangedEventHandler(OnGridLocationChangedEvent ev)
        {
            if (ev.Entity.HasComponent<GridDetectorComponent>())
            {
                UpdateGridDetection(ev.Entity, ev.Entity.GetComponent<GridComponent>(), true);
            }

            UpdateDetectPosition(ev.Entity, ev.Entity.GetComponent<GridComponent>(), true);
        }

        #endregion

        /// <summary>
        /// 해당 그리드 인덱스가 Observer에 의해 감시되고 있는지를 반환하고, 감시하는 Observer 들을 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsObserveIndex(in GridIndex index)
        {
            return m_GridObservers.ContainsKey(index);
        }
        /// <summary><inheritdoc cref="IsObserveIndex(in int)"/></summary>
        /// <param name="index"></param>
        /// <param name="observers"></param>
        /// <returns></returns>
        public bool IsObserveIndex(in GridIndex index, out UnsafeMultiHashMap<GridIndex, InstanceID>.Enumerator observers)
        {
            if (!IsObserveIndex(in index))
            {
                observers = default(UnsafeMultiHashMap<GridIndex, InstanceID>.Enumerator);
                return false;
            }

            observers = m_GridObservers.GetValuesForKey(index);
            return true;
        }

        /// <summary>
        /// Observer 로 지정된 Entity 를 업데이트합니다.
        /// </summary>
        /// <remarks>
        /// 대개 <seealso cref="GridDetectorAttribute"/> 를 상속받는 모든 Entity 들은 전부 Observer 가 됩니다.
        /// </remarks>
        /// <param name="entity"></param>
        /// <param name="gridSize"></param>
        /// <param name="postEvent"></param>
        public void UpdateGridDetection(Entity<IEntity> entity, in GridComponent gridSize, bool postEvent)
        {
            using (m_UpdateGridDetectionMarker.Auto())
            {
                ref GridDetectorComponent detector = ref entity.GetComponent<GridDetectorComponent>();
                // 새로운 그리드 Observation 을 위해 이 Entity 의 기존 Observe 그리드 인덱스를 제거합니다.
                ClearDetectorObserveIndices(ref m_GridObservers, entity.Idx, ref detector);

                int maxCount = detector.MaxDetectionIndicesCount;

                FixedList4096Bytes<GridIndex> buffer = new FixedList4096Bytes<GridIndex>();
                //int* buffer = stackalloc int[maxCount];
                //System.GetRange(in buffer, in maxCount, gridSize.positions[0].index, detector.DetectedRange, detector.m_IgnoreLayers, out int count);
                System.GetRange(entity.Idx, detector.DetectedRange, ref buffer);
                $"{buffer.Length} range : {entity.Name}, {detector.DetectedRange}".ToLog();
                FixedList512Bytes<InstanceID> newDetected = new FixedList512Bytes<InstanceID>();

                for (int i = 0; i < buffer.Length; i++)
                {
                    m_GridObservers.Add(buffer[i], entity.Idx);
                    detector.m_ObserveIndices.Add(buffer[i]);

                    Detection(entity, ref detector, buffer[i], ref newDetected, postEvent);
                }

                // 이 곳은 이전에 발견했으나, 이제는 조건이 달라져 발견하지 못한 Entity 들을 처리합니다.
                for (int i = 0; i < detector.m_Detected.Length; i++)
                {
                    if (newDetected.Contains(detector.m_Detected[i]))
                    {
                        //"already detect".ToLog();
                        continue;
                    }
                    else if (detector.m_DetectRemoveCondition.Execute(entity.ToEntity<IObject>(), out bool predicate) && predicate)
                    {
                        continue;
                    }

                    InstanceID targetID = detector.m_Detected[i];
                    Entity<IEntity> target = targetID.GetEntity<IEntity>();

                    // 만약 이전 타겟이 GridDetectorAttribute 를 상속받고있으면 내가 발견을 이제 못하게 됬음을 알립니다.
                    if (target.HasComponent<GridDetectorComponent>())
                    {
                        ref var targetDetector = ref target.GetComponent<GridDetectorComponent>();

                        if (targetDetector.m_TargetedBy.Contains(entity.Idx))
                        {
                            targetDetector.m_TargetedBy.Remove(entity.Idx);
                        }
                    }

                    "un detect".ToLog();
                    RemoveTargetedEntity(ref m_TargetedEntities, in targetID, entity.Idx);

                    if (postEvent)
                    {
                        m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(entity, target, false));
                    }
                }

                detector.m_Detected = newDetected;
            }
        }
        private void Detection(Entity<IEntity> entity, ref GridDetectorComponent detector, in GridIndex index, ref FixedList512Bytes<InstanceID> newDetected, bool postEvent)
        {
            if (!System.m_Indices.TryGetFirstValue(index.Index, out InstanceID targetID, out var iter))
            {
                return;
            }

            do
            {
                Entity<IEntity> target = targetID.GetEntity<IEntity>();
                if (targetID.Equals(entity.Idx) || !IsDetectorTriggerable(in detector, target)) continue;

                //EntityShortID targetShortID = targetID.GetShortID();

                if (detector.m_Detected.Contains(targetID))
                {
                    newDetected.Add(targetID);
                    continue;
                }

                Entity<IObject>
                    myDat = entity.ToEntity<IObject>(),
                    targetDat = target.ToEntity<IObject>();

                detector.m_OnDetectedPredicate.Execute(myDat, out bool predicate);
                if (!predicate)
                {
                    "predicate failed".ToLog();
                    continue;
                }

                //detector.m_Detected.Add(targetShortID);
                newDetected.Add(targetID);

                if (postEvent)
                {
                    m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(entity, target, true));
                }

                m_TargetedEntities.Add(targetID, entity.Idx);

                for (int i = 0; i < detector.m_OnDetected.Length; i++)
                {
                    detector.m_OnDetected[i].Execute(myDat, targetDat);
                }

                $"1. detect {entity.Name} spot {target.Name}".ToLog();
                if (target.HasComponent<GridDetectorComponent>())
                {
                    ref var targetDetector = ref target.GetComponent<GridDetectorComponent>();
                    //EntityShortID myShortID = entity.Idx.GetShortID();

                    if (!targetDetector.m_TargetedBy.Contains(entity.Idx))
                    {
                        targetDetector.m_TargetedBy.Add(entity.Idx);
                    }
                }

            } while (System.m_Indices.TryGetNextValue(out targetID, ref iter));
        }

        /// <summary>
        /// <see cref="GridSizeAttribute"/> 를 상속받는 모든 Entity 들을 Detector 에 의해 발견되었는 가를 연산합니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="gridSize"></param>
        /// <param name="postEvent"></param>
        public void UpdateDetectPosition(Entity<IEntity> entity, in GridComponent gridSize, bool postEvent)
        {
            using (m_UpdateDetectPositionMarker.Auto())
            {
                FixedList512Bytes<InstanceID>
                    detectors = new FixedList512Bytes<InstanceID>();

                for (int i = 0; i < gridSize.Indices.Length; i++)
                {
                    CheckObservers(entity, gridSize.Indices[i], ref detectors, postEvent);
                }

                bool entityHasDetector = entity.HasComponent<GridDetectorComponent>();

                //$"1. detect count {detectors.Length} : {m_TargetedEntities.CountValuesForKey(entity.Idx)}".ToLog();

                FixedList512Bytes<InstanceID>
                        unDetectors = new FixedList512Bytes<InstanceID>();
                InstanceID myShortID = entity.Idx;

                foreach (InstanceID detectorID in m_TargetedEntities.GetValuesForKey(entity.Idx))
                {
                    if (detectors.Contains(detectorID))
                    {
                        continue;
                    }
                    var detectorEntity = detectorID.GetEntity<IObject>();
                    ref var detector = ref detectorEntity.GetComponent<GridDetectorComponent>();
                    if (detector.m_DetectRemoveCondition.Execute(detectorEntity, out bool predicate) && predicate)
                    {
                        continue;
                    }

                    if (entityHasDetector)
                    {
                        ref var myDetector = ref entity.GetComponent<GridDetectorComponent>();
                        //EntityShortID detectorShortID = detectorID;

                        if (myDetector.m_TargetedBy.Contains(detectorID))
                        {
                            myDetector.m_TargetedBy.Remove(detectorID);
                        }
                    }

                    detector.m_Detected.Remove(myShortID);

                    if (postEvent)
                    {
                        m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(detectorID.GetEntity<IEntity>(), entity, false));
                    }

                    unDetectors.Add(detectorID);
                }

                //$"2. undetect count {unDetectors.Length}".ToLog();

                for (int i = 0; i < unDetectors.Length; i++)
                {
                    RemoveTargetedEntity(ref m_TargetedEntities, entity.Idx, unDetectors[i]);
                }
            }
        }
        private void CheckObservers(Entity<IEntity> entity, in GridIndex index, ref FixedList512Bytes<InstanceID> detectors, bool postEvent)
        {
            if (!m_GridObservers.TryGetFirstValue(index, out InstanceID observerID, out var iter))
            {
                return;
            }

            InstanceID targetShortID = entity.Idx;
            Entity<IObject> targetData = entity.ToEntity<IObject>();
            do
            {
                Entity<IObject> observer = observerID.GetEntity<IObject>();
                ref GridDetectorComponent detector = ref observer.GetComponent<GridDetectorComponent>();

                if (observerID.Equals(entity.Idx) || !IsDetectorTriggerable(in detector, entity)) continue;

                //$"detector search {observer.Name} ".ToLog();

                if (detector.m_Detected.Contains(targetShortID))
                {
                    //$"detector {observer.Name} already have {entity.Name}".ToLog();
                    detectors.Add(observerID);
                    continue;
                }

                detector.m_OnDetectedPredicate.Execute(targetData, out bool predicate);
                if (!predicate)
                {
                    "predicate failed".ToLog();
                    continue;
                }

                detector.m_Detected.Add(targetShortID);
                detectors.Add(observerID);

                if (postEvent)
                {
                    m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(observerID.GetEntity<IEntity>(), entity, true));
                }

                m_TargetedEntities.Add(entity.Idx, observerID);

                for (int i = 0; i < detector.m_OnDetected.Length; i++)
                {
                    detector.m_OnDetected[i].Execute(observer, targetData);
                }

                //$"2. detect {observer.Name} spot {entity.Name}".ToLog();
                if (targetData.HasComponent<GridDetectorComponent>())
                {
                    ref var targetDetector = ref targetData.GetComponent<GridDetectorComponent>();

                    if (!targetDetector.m_TargetedBy.Contains(targetShortID))
                    {
                        targetDetector.m_TargetedBy.Add(targetShortID);
                    }
                }

            } while (m_GridObservers.TryGetNextValue(out observerID, ref iter));
        }

        public void RemoveDetectorObserve(in Entity<IEntity> observer)
        {

        }

        #region Static Functions

        private static void RemoveTargetedEntity(ref UnsafeMultiHashMap<InstanceID, InstanceID> hashMap, in InstanceID targetID, in InstanceID detectorID)
        {
            //if (hashMap.CountValuesForKey(targetID) == 1)
            //{
            //    hashMap.Remove(targetID);
            //}
            //else
            //{
            //    hashMap.Remove(targetID, detectorID);
            //}

            RemoveValueAtHashMap(ref hashMap, in targetID, in detectorID);
        }
        private static bool IsDetectorTriggerable(in GridDetectorComponent detector, Entity<IEntity> target)
        {
            if (detector.m_TriggerOnly.Length == 0) return true;

            for (int i = 0; i < detector.m_TriggerOnly.Length; i++)
            {
                Hash temp = detector.m_TriggerOnly[i].Hash;

                if (target.Hash.Equals(temp))
                {
                    return !detector.m_TriggerOnlyInverse;
                }
            }
            return false;
        }
        private static void ClearDetectorObserveIndices(ref UnsafeMultiHashMap<GridIndex, InstanceID> hashMap, in InstanceID entityID, ref GridDetectorComponent detector)
        {
            for (int i = 0; i < detector.m_ObserveIndices.Length; i++)
            {
                RemoveValueAtHashMap(ref hashMap, detector.m_ObserveIndices[i], in entityID);
            }
            detector.m_ObserveIndices.Clear();
        }
        private static void RemoveValueAtHashMap<T, TA>(ref UnsafeMultiHashMap<T, TA> hashMap, in T key, in TA value)
            where T : unmanaged, IEquatable<T>
            where TA : unmanaged, IEquatable<TA>
        {
            if (hashMap.CountValuesForKey(key) == 1)
            {
                hashMap.Remove(key);
            }
            else
            {
                hashMap.Remove(key, value);
            }
        }

        #endregion
    }
    public struct GridDetectorComponent : IEntityComponent
    {
        private int m_DetectionRange;

        internal FixedList4096Bytes<GridIndex> m_ObserveIndices;

        internal FixedReferenceList64<EntityBase> m_TriggerOnly;
        internal bool m_TriggerOnlyInverse;

        internal FixedReferenceList64<TriggerPredicateAction> m_OnDetectedPredicate;
        internal FixedReferenceList64<TriggerPredicateAction> m_DetectRemoveCondition;
        internal FixedLogicTriggerAction8 m_OnDetected;

        /// <summary>
        /// 이 Detector 가 발견한 Entity 들을 담습니다.
        /// </summary>
        internal FixedList512Bytes<InstanceID> m_Detected;
        /// <summary>
        /// 이 Detector 가 다른 Entity 의 Detector 에 의해 발견되었으면 해당 발견자를 담습니다.
        /// </summary>
        internal FixedList512Bytes<InstanceID> m_TargetedBy;

        public int DetectedRange { get => m_DetectionRange; set => m_DetectionRange = value; }
        /// <summary>
        /// 이 Detector 가 최대로 감시할 수 있는 시스템 연산상 그리드 인덱스의 최대치를 반환합니다.
        /// </summary>
        public int MaxDetectionIndicesCount => CalculateMaxiumIndicesInRangeCount(m_DetectionRange);

        private static int CalculateMaxiumIndicesInRangeCount(in int range)
        {
            int height = ((range * 2) + 1);
            return height * height * height;
        }
    }
}
