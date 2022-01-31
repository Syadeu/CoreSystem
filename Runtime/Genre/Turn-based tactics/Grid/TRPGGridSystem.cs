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
using Syadeu.Mono;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Grid;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [SubSystem(typeof(DefaultPresentationGroup), typeof(WorldGridSystem))]
    public sealed class TRPGGridSystem : PresentationSystemEntity<TRPGGridSystem>,
        INotifySystemModule<TRPGGridObjectModule>
#if CORESYSTEM_SHAPES
        , INotifySystemModule<TRPGGridShapesModule>
#endif
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        //private LineRenderer 
        //    m_GridOutlineRenderer, m_GridPathlineRenderer;

        private NativeList<int3> 
            m_GridTempLocationHolder01,
            m_GridTempLocationHolder02,
            m_GridTempCoverables,
            m_GridTempMoveables, 
            m_GridTempOutcoasts;
        private NativeList<float3> 
            m_GridTempOutlines, m_GridTempPathlines;

        //private ComputeBuffer m_GridOutlineBuffer;
        //private Mesh m_OutlineMesh;

        private bool 
            m_IsDrawingGrids = false,
            m_IsDrawingPaths = false;

        private Unity.Profiling.ProfilerMarker
            m_DrawUICellMarker = new Unity.Profiling.ProfilerMarker($"{nameof(TRPGGridSystem)}.{nameof(DrawUICell)}"),
            //m_PlaceUICellMarker = new Unity.Profiling.ProfilerMarker($"{nameof(TRPGGridSystem)}.{nameof(PlaceUICell)}"),
            m_ClearUICellMarker = new Unity.Profiling.ProfilerMarker($"{nameof(TRPGGridSystem)}.{nameof(ClearUICell)}");

#if CORESYSTEM_HDRP
        private HDRPProjectionCamera m_GridOutlineCamera;
#endif

        public bool IsDrawingUIGrid => m_IsDrawingGrids;
        public bool ISDrawingUIPath => m_IsDrawingPaths;

        public NativeArray<int3>.ReadOnly CurrentMoveableTiles => m_GridTempMoveables.AsArray().AsReadOnly();
        public NativeArray<float3>.ReadOnly CurrentMoveableOutline => m_GridTempOutlines.AsArray().AsReadOnly();
        public NativeArray<float3>.ReadOnly CurrentPathline => m_GridTempPathlines.AsArray().AsReadOnly();

        private InputSystem m_InputSystem;
        private WorldGridSystem m_GridSystem;
        private RenderSystem m_RenderSystem;
        private NavMeshSystem m_NavMeshSystem;
        private EventSystem m_EventSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGSelectionSystem m_SelectionSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            //m_GridOutlineBuffer = new ComputeBuffer(128, 12, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);

//            m_OutlineMesh = new Mesh();

//            {
//                m_GridOutlineRenderer = CreateGameObject("Grid Outline Renderer", true).AddComponent<LineRenderer>();
//                m_GridOutlineRenderer.numCornerVertices = 0;
//                m_GridOutlineRenderer.numCapVertices = 1;
//                m_GridOutlineRenderer.alignment = LineAlignment.View;
//                m_GridOutlineRenderer.textureMode = LineTextureMode.Tile;

//                m_GridOutlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

//                m_GridOutlineRenderer.startWidth = CoreSystemSettings.Instance.m_TRPGGridLineWidth;
//                m_GridOutlineRenderer.material = CoreSystemSettings.Instance.m_TRPGGridLineMaterial;

//                m_GridOutlineRenderer.loop = true;
//                m_GridOutlineRenderer.positionCount = 0;

//#if CORESYSTEM_HDRP
//                m_GridOutlineRenderer.gameObject.layer = RenderSystem.ProjectionLayer;
//#endif
//            }

//            {
//                m_GridPathlineRenderer = CreateGameObject("Grid Pathline Renderer", true).AddComponent<LineRenderer>();
//                m_GridPathlineRenderer.numCornerVertices = 1;
//                m_GridPathlineRenderer.numCapVertices = 1;
//                m_GridPathlineRenderer.alignment = LineAlignment.View;
//                m_GridPathlineRenderer.textureMode = LineTextureMode.Tile;
//                m_GridPathlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
//                m_GridPathlineRenderer.receiveShadows = false;

//                m_GridPathlineRenderer.startWidth = CoreSystemSettings.Instance.m_TRPGGridPathLineWidth;
//                m_GridPathlineRenderer.endWidth = CoreSystemSettings.Instance.m_TRPGGridPathLineWidth;
//                m_GridPathlineRenderer.material = CoreSystemSettings.Instance.m_TRPGGridPathLineMaterial;

//                m_GridPathlineRenderer.loop = false;
//                m_GridPathlineRenderer.positionCount = 0;
//            }

            m_GridTempLocationHolder01 = new NativeList<int3>(512, AllocatorManager.Persistent);
            m_GridTempLocationHolder02 = new NativeList<int3>(512, AllocatorManager.Persistent);
            m_GridTempCoverables = new NativeList<int3>(512, Allocator.Persistent);
            m_GridTempMoveables = new NativeList<int3>(512, Allocator.Persistent);
            m_GridTempOutcoasts = new NativeList<int3>(512, Allocator.Persistent);
            m_GridTempOutlines = new NativeList<float3>(512, Allocator.Persistent);
            m_GridTempPathlines = new NativeList<float3>(512, Allocator.Persistent);

            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldGridSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, NavMeshSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);

            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGSelectionSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            //Destroy(m_GridOutlineRenderer.gameObject);
            //Destroy(m_GridPathlineRenderer.gameObject);

            m_EventSystem.RemoveEvent<OnShortcutStateChangedEvent>(OnShortcutStateChangedEventHandler);
            m_EventSystem.RemoveEvent<OnGridCellCursorOverrapEvent>(OnGridCellCursorOverrapEventHandler);
        }
        protected override void OnDispose()
        {
            m_GridTempLocationHolder01.Dispose();
            m_GridTempLocationHolder02.Dispose();
            m_GridTempCoverables.Dispose();
            m_GridTempMoveables.Dispose();
            m_GridTempOutcoasts.Dispose();
            m_GridTempOutlines.Dispose();
            m_GridTempPathlines.Dispose();

            m_InputSystem = null;
            m_GridSystem = null;
            m_RenderSystem = null;
            m_NavMeshSystem = null;
            m_EventSystem = null;

            m_TurnTableSystem = null;
            m_SelectionSystem = null;
        }

        #region Binds

        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }
        private void Bind(WorldGridSystem other)
        {
            m_GridSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }

        private void Bind(NavMeshSystem other)
        {
            m_NavMeshSystem = other;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnShortcutStateChangedEvent>(OnShortcutStateChangedEventHandler);
            m_EventSystem.AddEvent<OnGridCellCursorOverrapEvent>(OnGridCellCursorOverrapEventHandler);
        }

        private void Bind(TRPGTurnTableSystem other)
        {
            m_TurnTableSystem = other;
        }
        private void Bind(TRPGSelectionSystem other)
        {
            m_SelectionSystem = other;
        }

        #endregion

        #region Event Handlers

        private void OnShortcutStateChangedEventHandler(OnShortcutStateChangedEvent ev)
        {
            switch (ev.ShortcutType)
            {
                default:
                case UI.ShortcutType.None:
                    break;
                case UI.ShortcutType.Move:
                    m_GridSystem.EnableCursorObserve(ev.Enabled);
                    if (ev.Enabled)
                    {
                        DrawUICell(m_TurnTableSystem.CurrentTurn);
                    }
                    else
                    {
                        ClearUICell();
                        ClearUIPath();
                    }

                    break;
                case UI.ShortcutType.Attack:
                    break;
            }
        }
        private void OnGridCellCursorOverrapEventHandler(OnGridCellCursorOverrapEvent ev)
        {
            var grid = m_TurnTableSystem.CurrentTurn.GetComponent<GridComponent>();

            if (!m_GridTempMoveables.Contains(ev.Index.Location))
            {
                ClearUIPath();
                return;
            }

            DrawUIPath(grid.Indices[0], ev.Index);
        }
        private void TRPGGridCellUIPressedEventHandler(OnGridCellPreseedEvent ev)
        {
            if (!m_GridTempMoveables.Contains(ev.Index.Location))
            {
                return;
            }

            MoveToCell(m_TurnTableSystem.CurrentTurn.Idx, ev.Index);
        }

        private bool m_DrawMesh = false;

        #endregion

        protected override PresentationResult AfterPresentation()
        {
            //if (m_DrawMesh)
            //{
            //    Graphics.DrawMesh(m_OutlineMesh, Matrix4x4.identity, CoreSystemSettings.Instance.m_TRPGGridLineMaterial, 0);
            //}

            return base.AfterPresentation();
        }

        #endregion

        #region Math

        private void GetMoveablePositions(in InstanceID entity,
            ref NativeList<int3> gridPositions)
        {
            var turnPlayer = entity.GetComponent<TurnPlayerComponent>();
            var gridsize = entity.GetComponent<GridComponent>();

            gridPositions.Clear();
            FixedList4096Bytes<GridIndex> foundPath = new FixedList4096Bytes<GridIndex>();
            foreach (var item in m_GridSystem.GetRange(in entity, new int3(turnPlayer.ActionPoint, 0, turnPlayer.ActionPoint)))
            {
                if (!gridsize.IsMyIndex(item) && m_GridSystem.HasEntityAt(item))
                {
                    continue;
                }
                else if (!m_GridSystem.GetPath(gridsize.Indices[0], item, ref foundPath, out int tileCount))
                {
                    continue;
                }
                else if (tileCount > turnPlayer.ActionPoint)
                {
                    continue;
                }

                gridPositions.Add(item.Location);
            }
        }
        public void GetMoveablePositions(in InstanceID entity,
            ref FixedList4096Bytes<GridIndex> gridPositions)
        {
            var turnPlayer = entity.GetComponent<TurnPlayerComponent>();
            var gridsize = entity.GetComponent<GridComponent>();

            FixedList4096Bytes<GridIndex> list = new FixedList4096Bytes<GridIndex>();
            // TODO : Temp code
            m_GridSystem.GetRange(in entity, new int3(turnPlayer.ActionPoint, 0, turnPlayer.ActionPoint), ref list);

            gridPositions.Clear();
            for (int i = 0; i < list.Length; i++)
            {
                if (m_GridSystem.HasEntityAt(list[i]))
                {
                    continue;
                }
                else if (!m_GridSystem.HasPath(gridsize.Indices[0], list[i], out int pathCount) ||
                    pathCount > turnPlayer.ActionPoint)
                {
                    continue;
                }

                gridPositions.Add((list[i]));
                //$"{list[i].Location} added".ToLog();
            }
        }
        public Direction GetCoverableDirection(in GridIndex index)
        {
            WorldGridSystem.EntitiesAtIndexEnumerator iter = default;
            Direction result = Direction.NONE;

            for (int a = 2; a < 6; a++)
            {
                if (!m_GridSystem.TryGetDirection(index, (Direction)(1 << a), out var tempIndex))
                {
                    continue;
                }
                else if (!m_GridSystem.TryGetEntitiesAt(tempIndex, out iter))
                {
                    continue;
                }

                foreach (var entity in iter)
                {
                    GridComponent grid = entity.GetComponentReadOnly<GridComponent>();
                    if ((grid.ObstacleType & GridComponent.Obstacle.Block) 
                            != GridComponent.Obstacle.Block)
                    {
                        continue;
                    }

                    result |= (Direction)(1 << a);
                    break;
                }
            }

            return result;
        }

        #endregion

        #region UI

        private void DrawUICell(Entity<IEntityData> entity)
        {
            using (m_DrawUICellMarker.Auto())
            {
                if (m_IsDrawingGrids)
                {
                    ClearUICell();
                }

                GetMoveablePositions(entity.Idx, ref m_GridTempMoveables);
                m_GridSystem.GetOutcoastLocations(m_GridTempMoveables, ref m_GridTempOutcoasts);
                m_GridSystem.GetOutcoastVertices(m_GridTempOutcoasts, ref m_GridTempOutlines, m_GridTempMoveables);
                //m_GridSystem.GetOutcoastVertices(m_GridTempMoveables, ref m_GridTempOutlines);
                //for (int i = 0; i < m_GridTempOutcoasts.Length; i++)
                //{
                //    $"out loc:: {m_GridTempOutcoasts[i]}".ToLog();
                //}
                $"out loc count : {m_GridTempOutcoasts.Length}, vert : {m_GridTempOutlines.Length}".ToLog();

                for (int i = 0; i < m_GridTempOutcoasts.Length; i++)
                {
                    Direction direction = GetCoverableDirection(m_GridSystem.LocationToIndex(m_GridTempOutcoasts[i]));
                    if (direction == 0)
                    {
                        continue;
                    }

                    $"{TypeHelper.Enum<Direction>.ToString(direction)} at {m_GridTempOutcoasts[i]}".ToLog();
                }

                GridComponent gridSize = entity.GetComponentReadOnly<GridComponent>();

#if CORESYSTEM_HDRP
                m_GridOutlineCamera = m_RenderSystem.GetProjectionCamera(
                    CoreSystemSettings.Instance.m_TRPGGridLineMaterial,
                    CoreSystemSettings.Instance.m_TRPGGridProjectionTexture);
                m_GridOutlineCamera.SetPosition(m_GridSystem.IndexToPosition(gridSize.Indices[0]));
#endif
                m_EventSystem.AddEvent<OnGridCellPreseedEvent>(TRPGGridCellUIPressedEventHandler);

                m_IsDrawingGrids = true;
            }
        }
        private void ClearUICell()
        {
            using (m_ClearUICellMarker.Auto())
            {
                if (!m_IsDrawingGrids) return;

                //m_GridSystem.ClearUICell();
                m_GridTempMoveables.Clear();
                m_GridTempOutcoasts.Clear();
                m_GridTempOutlines.Clear();

                //m_ShapesOutlinePath.ClearAllPoints();
                //m_GridOutlineRenderer.positionCount = 0;

#if CORESYSTEM_HDRP
                m_GridOutlineCamera.Dispose();
                m_GridOutlineCamera = null;
#endif
                m_EventSystem.RemoveEvent<OnGridCellPreseedEvent>(TRPGGridCellUIPressedEventHandler);

                m_IsDrawingGrids = false;
            }
        }

        private void DrawUIPath(in FixedList4096Bytes<GridIndex> path, float heightOffset = .5f)
        {
            if (m_IsDrawingPaths)
            {
                ClearUIPath();
            }

            m_GridTempPathlines.Clear();
            float3 offset = new float3(0, heightOffset, 0);

            //m_GridPathlineRenderer.positionCount = path.Length;
            for (int i = 0; i < path.Length; i++)
            {
                m_GridTempPathlines.Add(m_GridSystem.IndexToPosition(path[i]) + offset);
            }
            //m_GridPathlineRenderer.SetPositions(m_GridTempPathlines);

            m_IsDrawingPaths = true;
        }
        private void DrawUIPath(in GridIndex from, in GridIndex to)
        {
            if (m_IsDrawingPaths)
            {
                m_GridTempPathlines.Clear();
                //m_GridPathlineRenderer.positionCount = 0;
            }

            FixedList4096Bytes<GridIndex> foundPath = new FixedList4096Bytes<GridIndex>();
            if (!m_GridSystem.GetPath(in from, in to, ref foundPath, out _)) return;

            for (int i = 0; i < foundPath.Length; i++)
            {
                m_GridTempPathlines.Add(m_GridSystem.IndexToPosition(foundPath[i]));
            }

            m_IsDrawingPaths = true;
        }
        private void ClearUIPath()
        {
            if (!m_IsDrawingPaths) return;

            m_GridTempPathlines.Clear();
            //m_GridPathlineRenderer.positionCount = 0;

            m_IsDrawingPaths = false;
        }

        #endregion

        public ActorEventHandler MoveToCell(InstanceID entity, GridIndex position)
        {
            FixedList4096Bytes<GridIndex> path = new FixedList4096Bytes<GridIndex>();
            if (!m_GridSystem.GetPath(entity, position, ref path, out int tileCount))
            {
                "path error not found".ToLogError();
                return ActorEventHandler.Empty;
            }

            ActorEventHandler handler = m_NavMeshSystem.MoveTo(entity.GetEntity<IEntity>(),
                path, new ActorMoveEvent(entity, 1));

            ref TurnPlayerComponent turnPlayer = ref entity.GetComponent<TurnPlayerComponent>();
            int requireAp = tileCount;

            turnPlayer.ActionPoint -= requireAp;

            return handler;
        }
    }
}