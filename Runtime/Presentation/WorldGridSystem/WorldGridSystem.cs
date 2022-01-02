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
using Syadeu.Presentation.Grid.LowLevel;
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
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;
        public override bool EnableAfterTransformPresentation => true;

        private WorldGrid m_Grid;
        private bool m_GridInitialized;
        private AtomicSafeBoolen m_RequireGridUpdate;

        private EntityComponentSystem m_ComponentSystem;
        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_GridInitialized = false;

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

        }
        private void M_ComponentSystem_OnComponentRemove(InstanceID arg1, Type arg2)
        {

        }

        #endregion

        #region Event Handlers

        private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        {

        }

        #endregion

        protected override PresentationResult AfterTransformPresentation()
        {
            ScheduleGridUpdate();

            return base.AfterTransformPresentation();
        }

        private void ScheduleGridUpdate()
        {
            if (!m_GridInitialized || !m_RequireGridUpdate.Value) return;

            UpdateGridJob job = new UpdateGridJob
            {

            };

            ScheduleAt<UpdateGridJob, GridComponent>(JobPosition.AfterTransform, job);

            m_RequireGridUpdate.Value = false;
        }
        private struct UpdateGridJob : IJobParallelForEntities<GridComponent>
        {
            [ReadOnly] public WorldGrid grid;

            public void Execute(in InstanceID entity, ref GridComponent component)
            {

            }
        }

        #endregion

        public void InitializeGrid(in AABB aabb, in float cellSize)
        {
            m_Grid = new WorldGrid(in aabb, in cellSize);

            m_GridInitialized = true;
        }

        public void UpdateEntity(InstanceID entity)
        {
            if (!m_GridInitialized) return;

            m_Grid.Update(entity);
        }
        public void AddEntity(InstanceID entity)
        {
#if DEBUG_MODE
            if (!entity.IsEntity<IEntity>())
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"This entity is not inherted from {nameof(EntityBase)}. " +
                    $"This is not allowed.");

                return;
            }
            else if (m_ComponentSystem.HasComponent<GridComponent>(in entity))
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"This entity already registered.");

                return;
            }
#endif
            m_ComponentSystem.AddComponent<GridComponent>(in entity);
            m_RequireGridUpdate.Value = true;
        }
        public void RemoveEntity(InstanceID entity)
        {
#if DEBUG_MODE
            if (!entity.IsEntity<IEntity>())
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"This entity is not inherted from {nameof(EntityBase)}. " +
                    $"This is not allowed.");

                return;
            }
            else if (m_ComponentSystem.HasComponent<GridComponent>(in entity))
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"This entity already registered.");

                return;
            }
#endif
            m_ComponentSystem.RemoveComponent<GridComponent>(in entity);
            m_RequireGridUpdate.Value = true;
        }
    }

    [BurstCompatible]
    internal unsafe struct WorldGrid : IDisposable
    {
        private readonly short m_CheckSum;

        private AABB m_AABB;
        private float m_CellSize;

        private UnsafeMultiHashMap<int, InstanceID> m_Indices;
        private UnsafeHashMap<InstanceID, int> m_Entities;

        public int length
        {
            get
            {
                float3 size = m_AABB.size;
                int
                    xSize = Convert.ToInt32(math.floor(size.x / m_CellSize)),
                    zSize = Convert.ToInt32(math.floor(size.z / m_CellSize));
                return xSize * zSize;
            }
        }
        public float cellSize { get => m_CellSize; set => m_CellSize = value; }
        public int2 gridSize
        {
            get
            {
                float3 size = m_AABB.size;
                return new int2(
                    Convert.ToInt32(math.floor(size.x / m_CellSize)),
                    Convert.ToInt32(math.floor(size.z / m_CellSize)));
            }
        }

        public UnsafeMultiHashMap<int, InstanceID> Entries => m_Indices;

        internal WorldGrid(in AABB aabb, in float cellSize)
        {
            this = default(WorldGrid);

            m_CheckSum = CollectionUtility.CreateHashInt16();

            m_AABB = aabb;
            m_CellSize = cellSize;

            m_Indices = new UnsafeMultiHashMap<int, InstanceID>(length, AllocatorManager.Persistent);
            m_Entities = new UnsafeHashMap<InstanceID, int>(length, AllocatorManager.Persistent);
        }

        #region Index

        public int3 PositionToLocation(in float3 position)
        {
            int3 location;
            unsafe
            {
                BurstGridMathematics.positionToLocation(in m_AABB, in m_CellSize, in position, &location);
            }
            return location;
        }
        public int PositionToIndex(in float3 position)
        {
            int index;
            unsafe
            {
                BurstGridMathematics.positionToIndex(in m_AABB, in m_CellSize, in position, &index);
            }
            return index;
        }
        public int LocationToIndex(in int3 location)
        {
            int index;
            unsafe
            {
                BurstGridMathematics.locationToIndex(in m_AABB, in m_CellSize, in location, &index);
            }
            return index;
        }
        public float3 LocationToPosition(in int3 location)
        {
            float3 position;
            unsafe
            {
                BurstGridMathematics.locationToPosition(in m_AABB, in m_CellSize, in location, &position);
            }
            return position;
        }
        public int3 IndexToLocation(in int index)
        {
            int3 location;
            unsafe
            {
                BurstGridMathematics.indexToLocation(in m_AABB, in m_CellSize, in index, &location);
            }
            return location;
        }
        public float3 IndexToPosition(in int index)
        {
            float3 position;
            unsafe
            {
                BurstGridMathematics.indexToPosition(in m_AABB, in m_CellSize, in index, &position);
            }
            return position;
        }

        #endregion

        public bool Contains(in int index)
        {
            bool result;
            BurstGridMathematics.containIndex(in m_AABB, in m_CellSize, in index, &result);
            return result;
        }
        public bool Contains(in int3 location)
        {
            bool result;
            BurstGridMathematics.containLocation(in m_AABB, in m_CellSize, in location, &result);
            return result;
        }

        public void Add(in int index, in InstanceID entity)
        {
            m_Indices.Add(index, entity);
            m_Entities.Add(entity, index);
        }
        public void Add(in InstanceID entity)
        {
            int index = PositionToIndex(entity.GetTransformWithoutCheck().position);

            m_Indices.Add(index, entity);
            m_Entities.Add(entity, index);
        }
        public void Remove(in int index, in InstanceID entity)
        {
            m_Indices.Remove(index, entity);
            m_Entities.Remove(entity);
        }
        public void Remove(in InstanceID entity)
        {
            int index = m_Entities[entity];

            m_Indices.Remove(index, entity);
            m_Entities.Remove(entity);
        }
        public bool HasEntityAt(in int3 location)
        {
            return m_Indices.ContainsKey(LocationToIndex(in location));
        }
        public int GetIndex(in InstanceID entity)
        {
            return m_Entities[entity];
        }

        public void GetNearbyEntities(
            in int index, in int xzRange, in int yRange, 
            ref UnsafeAllocator<InstanceID> output)
        {
            if (!output.IsCreated)
            {
                throw new Exception();
            }
            if (!Contains(in index))
            {
                throw new Exception();
            }

            int3 
                location = IndexToLocation(index),
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

                m_Indices.TryGetFirstValue(LocationToIndex(buffer[i]), out InstanceID entity, out var iter);
                do
                {
                    output[added++] = entity;
                } while (added < output.Length && m_Indices.TryGetNextValue(out entity, ref iter));
            }
        }

        public void Update(in InstanceID entity)
        {
            Remove(entity);
            Add(entity);
        }
        public JobHandle Update()
        {
            var entries = m_Entities.GetKeyArray(AllocatorManager.TempJob);
            UpdateGridJob job = new UpdateGridJob(this, entries);

            return job.Schedule(entries.Length, 64);
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
        [BurstCompile(CompileSynchronously = true)]
        private struct UpdateGridJob : IJobParallelFor
        {
            [ReadOnly, DeallocateOnJobCompletion]
            private NativeArray<InstanceID> entries;
            [ReadOnly]
            private WorldGrid grid;
            [ReadOnly]
            private EntityTransformHashMap m_TrHashMap;

            [WriteOnly]
            private UnsafeMultiHashMap<int, InstanceID>.ParallelWriter indices;
            [WriteOnly]
            private UnsafeHashMap<InstanceID, int>.ParallelWriter entities;

            public UpdateGridJob(WorldGrid worldGrid, NativeArray<InstanceID> entries)
            {
                this.entries = entries;
                grid = worldGrid;
                m_TrHashMap = EntityTransformStatic.GetHashMap();

                worldGrid.m_Indices.Clear(); worldGrid.m_Entities.Clear();
                indices = worldGrid.m_Indices.AsParallelWriter();
                entities = worldGrid.m_Entities.AsParallelWriter();
            }

            public void Execute(int i)
            {
                int index = grid.PositionToIndex(m_TrHashMap.GetTransform(entries[i]).position);
                indices.Add(index, entries[i]);
                entities.TryAdd(entries[i], index);
            }
        }

        public void Dispose()
        {
            m_Indices.Dispose();
            m_Entities.Dispose();
        }
    }

    public struct GridComponent : IEntityComponent
    {
        internal bool m_Initialized;
        internal int m_Index;

        public int index => m_Index;
    }

    public sealed class GridAttribute : AttributeBase
    {

    }
}
