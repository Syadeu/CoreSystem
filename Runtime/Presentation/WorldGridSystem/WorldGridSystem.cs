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
using Syadeu.Collections.Threading;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Grid.LowLevel;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_GridInitialized = false;

            RequestSystem<DefaultPresentationGroup, EntityComponentSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            m_ComponentSystem.OnComponentAdded -= M_ComponentSystem_OnComponentAdded;
            m_ComponentSystem.OnComponentRemove -= M_ComponentSystem_OnComponentRemove;
        }
        protected override void OnDispose()
        {
            m_ComponentSystem = null;
        }

        #region Binds

        private void Bind(EntityComponentSystem other)
        {
            m_ComponentSystem = other;

            m_ComponentSystem.OnComponentAdded += M_ComponentSystem_OnComponentAdded;
            m_ComponentSystem.OnComponentRemove += M_ComponentSystem_OnComponentRemove;
        }

        private void M_ComponentSystem_OnComponentAdded(InstanceID arg1, Type arg2)
        {

        }
        private void M_ComponentSystem_OnComponentRemove(InstanceID arg1, Type arg2)
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
    public struct WorldGrid : IDisposable
    {
        private readonly short m_CheckSum;

        private AABB m_AABB;
        private float m_CellSize;

        private UnsafeMultiHashMap<int, InstanceID> m_Entries;

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

        internal WorldGrid(in AABB aabb, in float cellSize)
        {
            this = default(WorldGrid);

            m_CheckSum = CollectionUtility.CreateHashInt16();

            m_AABB = aabb;
            m_CellSize = cellSize;

            m_Entries = new UnsafeMultiHashMap<int, InstanceID>(length, AllocatorManager.Persistent);
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
            unsafe
            {
                BurstGridMathematics.containIndex(in m_AABB, in m_CellSize, in index, &result);
            }
            return result;
        }
        public bool Contains(in int3 location)
        {
            bool result;
            unsafe
            {
                BurstGridMathematics.containLocation(in m_AABB, in m_CellSize, in location, &result);
            }
            return result;
        }

        public void Add(int index, InstanceID entity)
        {
            m_Entries.Add(index, entity);
        }

        public void Dispose()
        {
            m_Entries.Dispose();
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
