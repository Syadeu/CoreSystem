﻿// Copyright 2021 Seung Ha Kim
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
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Grid.LowLevel;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    public sealed class WorldGridSystem : PresentationSystemEntity<WorldGridSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private WorldGrid m_Grid;
        private bool m_InitializedGrid;
        private NativeMultiHashMap<int, InstanceID> m_GridEntities;

        private EntityComponentSystem m_ComponentSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_InitializedGrid = false;
            m_GridEntities = new NativeMultiHashMap<int, InstanceID>(1024, AllocatorManager.Persistent);

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
            m_GridEntities.Dispose();

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

        #endregion

        public void InitializeGrid(in AABB aabb, in float cellSize)
        {
            m_Grid = new WorldGrid(in aabb, in cellSize);

            m_InitializedGrid = true;
        }

        public void AddEntity(InstanceID entity)
        {
            if (!entity.IsEntity())
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "");

                return;
            }
            else if (m_ComponentSystem.HasComponent<GridComponent>(in entity))
            {

                return;
            }

            m_ComponentSystem.AddComponent<GridComponent>(in entity);
        }
        public void RemoveEntity(InstanceID entity)
        {

        }
    }

    public struct WorldGrid
    {
        public struct Data
        {
            public IntPtr data;
        }

        private AABB m_AABB;
        private float m_CellSize;

        public float cellSize { get => m_CellSize; set => m_CellSize = value; }

        public WorldGrid(in AABB aabb, in float cellSize)
        {
            m_AABB = aabb;
            m_CellSize = cellSize;
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
    }

    public struct GridComponent : IEntityComponent
    {
        internal int m_Index;

        public int index => m_Index;
    }

    public sealed class GridAttribute : AttributeBase
    {

    }
}
