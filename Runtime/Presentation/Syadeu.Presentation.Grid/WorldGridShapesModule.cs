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
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Render;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Grid
{
    internal sealed class WorldGridShapesModule : PresentationSystemModule<WorldGridSystem>
    {
#if CORESYSTEM_SHAPES
        private NativeHashMap<GridIndex, Entity<IEntity>> m_PlacedCellUIEntities;

        private bool m_DrawGrid;

        private Unity.Profiling.ProfilerMarker
            m_PlaceUICell = new Unity.Profiling.ProfilerMarker($"{nameof(WorldGridSystem)}.{nameof(WorldGridShapesModule)}.{nameof(PlaceUICell)}");

        private RenderSystem m_RenderSystem;
        private InputSystem m_InputSystem;
        private EntityRaycastSystem m_RaycastSystem;
        private EventSystem m_EventSystem;
        private LevelDesignSystem m_LevelDesignSystem;
        private SceneSystem m_SceneSystem;

        protected override void OnInitialize()
        {
            m_DrawGrid = true;

            m_PlacedCellUIEntities = new NativeHashMap<GridIndex, Entity<IEntity>>(1024, AllocatorManager.Persistent);

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntityRaycastSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<LevelDesignPresentationGroup, LevelDesignSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
        }

        #region Binds

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            m_RenderSystem.OnRender += M_RenderSystem_OnRender;
        }
        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }
        private void Bind(EntityRaycastSystem other)
        {
            m_RaycastSystem = other;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }
        private void Bind(LevelDesignSystem other)
        {
            m_LevelDesignSystem = other;
        }
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;
            m_SceneSystem.OnSceneLoadCall += M_SceneSystem_OnSceneLoadCall;
        }

        #endregion

        protected override void OnShutDown()
        {
            m_RenderSystem.OnRender -= M_RenderSystem_OnRender;

            m_SceneSystem.OnSceneLoadCall -= M_SceneSystem_OnSceneLoadCall;
        }
        protected override void OnDispose()
        {
            m_PlacedCellUIEntities.Dispose();

            m_RenderSystem = null;
            m_InputSystem = null;
            m_RaycastSystem = null;
            m_EventSystem = null;
            m_LevelDesignSystem = null;
        }

        #region Event Handlers

        private void M_RenderSystem_OnRender(UnityEngine.Rendering.ScriptableRenderContext arg1, Camera arg2)
        {
            if (!m_DrawGrid) return;

            //using (Shapes.Draw.Command(arg2))
            //{
            //    //DrawGridGL(System.Grid, .05f);
            //    //DrawOcc(arg2);
            //    DrawIndices(arg2);
            //}
        }
        private void M_SceneSystem_OnSceneLoadCall()
        {
            m_DrawGrid = false;
        }

        #endregion

        static void DrawGridGL(WorldGrid grid, float thickness)
        {
            const float yOffset = .15f;
            int3 gridSize = grid.gridSize;

            float3 minPos = grid.IndexToPosition(0);
            minPos.x -= grid.cellSize * .5f;
            minPos.z += grid.cellSize * .5f;

            minPos.y = 0;

            float3 maxPos = grid.LocationToPosition(gridSize);
            maxPos.x -= grid.cellSize * .5f;
            maxPos.z += grid.cellSize * .5f;

            for (int z = 0; z < gridSize.z + 2; z++)
            {
                for (int x = 0; x < gridSize.x + 2; x++)
                {
                    float3
                        p1 = new float3(
                            minPos.x,
                            minPos.y + yOffset,
                            minPos.z - (grid.cellSize * z)),
                        p2 = new float3(
                            maxPos.x + grid.cellSize,
                            minPos.y + yOffset,
                            minPos.z - (grid.cellSize * z)),
                        p3 = new float3(
                            minPos.x + (grid.cellSize * x),
                            minPos.y + yOffset,
                            minPos.z),
                        p4 = new float3(
                            minPos.x + (grid.cellSize * x),
                            minPos.y + yOffset,
                            maxPos.z - grid.cellSize)
                        ;

                    Shapes.Draw.Line(p1, p2);
                    Shapes.Draw.Line(p3, p4);
                }
            }
        }
        void DrawOcc(Camera cam)
        {
            System.CompleteGridJob();

            float3 camForward = cam.transform.forward;
            var arr = System.m_Indices.GetKeyArray(AllocatorManager.Temp);
            for (int i = 0; i < arr.Length; i++)
            {
                //if (System.m_Indices.CountValuesForKey(arr[i]) == 0) continue;

                var pos = System.Grid.IndexToPosition(arr[i]);

                float3 target = pos + new float3(0, .15f, 0);
                Shapes.Draw.Rectangle(target, Vector3.up, System.Grid.cellSize, System.Grid.cellSize, new Color(1, 1, 1, .5f));

                string nameSum = string.Empty;
                foreach (var entity in System.m_Indices.GetValuesForKey(arr[i]))
                {
                    nameSum += entity.GetEntity().Target.Name + ", ";
                }
                Shapes.Draw.Text(target, camForward, nameSum, 6f, Color.red);
            }

            arr.Dispose();
        }
        void DrawIndices(Camera cam)
        {
            System.CompleteGridJob();

            float3 camForward = cam.transform.forward;
            GridDetectorModule detectorModule = System.GetModule<GridDetectorModule>();
            System.Grid.GetMinMaxLocation(out int3 min, out int3 max);
            for (int x = min.x; x < max.x; x++)
            {
                for (int z = min.z; z < max.z; z++)
                {
                    var loc = new int3(x, 0, z);
                    var pos = System.Grid.LocationToPosition(loc);

                    float3 target = pos + new float3(0, .15f, 0);
                    GridIndex index = new GridIndex(System.Grid, System.Grid.LocationToIndex(loc));

                    if (detectorModule.GridObservers.ContainsKey(index))
                    {
                        detectorModule.GridObservers.TryGetFirstValue(index, out var ob, out _);

                        //Shapes.Draw.Text(target, camForward, $"{loc.x},{loc.y},{loc.z}::{ob.GetEntity().Name}({detectorModule.GridObservers.CountValuesForKey(index)})", 3.5f, Color.red);
                        Shapes.Draw.Text(target, camForward, $"{ob.GetEntity().Name}({detectorModule.GridObservers.CountValuesForKey(index)})", 3.5f, Color.red);
                    }
                    else
                    {
                        Shapes.Draw.Text(target, camForward, $"{loc.x},{loc.y},{loc.z}", 3.5f, Color.white);
                    }
                }
            }
        }

        [Obsolete]
        public Entity<IEntity> PlaceUICell(GridIndex position, float heightOffset = .25f)
        {
            using (m_PlaceUICell.Auto())
            {
                if (m_PlacedCellUIEntities.TryGetValue(position, out var exist))
                {
                    return exist;
                }
                //#if DEBUG_MODE
                //                if (GridMap.m_CellUIPrefab.IsEmpty() || !GridMap.m_CellUIPrefab.IsValid())
                //                {
                //                    CoreSystem.Logger.LogError(Channel.Entity,
                //                        $"Cannot place grid ui cell at {position} because there\'s no valid CellEntity " +
                //                        $"in {nameof(GridMapAttribute)}({GridMap.Name}, MapData: {GridMap.ParentEntity.Name})");

                //                    return Entity<IEntity>.Empty;
                //                }
                //#endif

                return Entity<IEntity>.Empty;

            }
        }
#endif
    }
}
