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
using Syadeu.Presentation.Grid;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Render;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [SubSystem(typeof(DefaultPresentationGroup), typeof(WorldGridSystem))]
    public sealed class TRPGGridSystem : PresentationSystemEntity<TRPGGridSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => true;

        private LineRenderer 
            m_GridOutlineRenderer, m_GridPathlineRenderer;

        private NativeList<GridIndex> m_GridTempMoveables;
        private NativeList<Vector3> 
            m_GridTempOutlines, m_GridTempPathlines;

        //private ComputeBuffer m_GridOutlineBuffer;
        private Mesh m_OutlineMesh;

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

        private InputSystem m_InputSystem;
        private WorldGridSystem m_GridSystem;
        private RenderSystem m_RenderSystem;
        private NavMeshSystem m_NavMeshSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            //m_GridOutlineBuffer = new ComputeBuffer(128, 12, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);

            m_OutlineMesh = new Mesh();

            {
                m_GridOutlineRenderer = CreateGameObject("Grid Outline Renderer", true).AddComponent<LineRenderer>();
                m_GridOutlineRenderer.numCornerVertices = 0;
                m_GridOutlineRenderer.numCapVertices = 1;
                m_GridOutlineRenderer.alignment = LineAlignment.View;
                m_GridOutlineRenderer.textureMode = LineTextureMode.Tile;

                m_GridOutlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                m_GridOutlineRenderer.startWidth = CoreSystemSettings.Instance.m_TRPGGridLineWidth;
                m_GridOutlineRenderer.material = CoreSystemSettings.Instance.m_TRPGGridLineMaterial;

                m_GridOutlineRenderer.loop = true;
                m_GridOutlineRenderer.positionCount = 0;

#if CORESYSTEM_HDRP
                m_GridOutlineRenderer.gameObject.layer = RenderSystem.ProjectionLayer;
#endif
            }

            {
                m_GridPathlineRenderer = CreateGameObject("Grid Pathline Renderer", true).AddComponent<LineRenderer>();
                m_GridPathlineRenderer.numCornerVertices = 1;
                m_GridPathlineRenderer.numCapVertices = 1;
                m_GridPathlineRenderer.alignment = LineAlignment.View;
                m_GridPathlineRenderer.textureMode = LineTextureMode.Tile;
                m_GridPathlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                m_GridPathlineRenderer.receiveShadows = false;

                m_GridPathlineRenderer.startWidth = CoreSystemSettings.Instance.m_TRPGGridPathLineWidth;
                m_GridPathlineRenderer.endWidth = CoreSystemSettings.Instance.m_TRPGGridPathLineWidth;
                m_GridPathlineRenderer.material = CoreSystemSettings.Instance.m_TRPGGridPathLineMaterial;

                m_GridPathlineRenderer.loop = false;
                m_GridPathlineRenderer.positionCount = 0;
            }

            m_GridTempMoveables = new NativeList<GridIndex>(512, Allocator.Persistent);
            m_GridTempOutlines = new NativeList<Vector3>(512, Allocator.Persistent);
            m_GridTempPathlines = new NativeList<Vector3>(512, Allocator.Persistent);

            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldGridSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, NavMeshSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnDispose()
        {
            Destroy(m_GridOutlineRenderer.gameObject);
            Destroy(m_GridPathlineRenderer.gameObject);

            //m_GridOutlineBuffer.Release();

            m_GridTempMoveables.Dispose();
            m_GridTempOutlines.Dispose();
            m_GridTempPathlines.Dispose();

            m_InputSystem = null;
            m_GridSystem = null;
            m_RenderSystem = null;
            m_NavMeshSystem = null;
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

        #endregion

        private bool m_DrawMesh = false;

        protected override PresentationResult AfterPresentation()
        {
            if (m_DrawMesh)
            {
                Graphics.DrawMesh(m_OutlineMesh, Matrix4x4.identity, CoreSystemSettings.Instance.m_TRPGGridLineMaterial, 0);
            }

            return base.AfterPresentation();
        }

        #endregion

        public void GetMoveablePositions(in InstanceID entity,
            ref NativeList<GridIndex> gridPositions)
        {
            var turnPlayer = entity.GetComponent<TurnPlayerComponent>();
            var gridsize = entity.GetComponent<GridComponent>();

            FixedList4096Bytes<GridIndex> list = new FixedList4096Bytes<GridIndex>();
            m_GridSystem.GetRange(in entity, turnPlayer.ActionPoint, ref list);

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
            }
        }
        public void CalculateMoveableOutlineVertices(
            in InstanceID entity,
            NativeArray<GridIndex> moveables,
            ref NativeList<Vector3> vertices, float heightOffset = .25f)
        {
            var gridsize = entity.GetComponent<GridComponent>();
            float cellsize = m_GridSystem.CellSize * .5f;

            float3
                upleft = new float3(-cellsize, heightOffset, cellsize),
                upright = new float3(cellsize, heightOffset, cellsize),
                downleft = new float3(-cellsize, heightOffset, -cellsize),
                downright = new float3(cellsize, heightOffset, -cellsize);

            vertices.Clear();
            float3 gridPos;

            if (moveables.Length == 0)
            {
                gridPos = m_GridSystem.IndexToPosition(gridsize.Indices[0]);

                vertices.Add(gridPos + upright);
                vertices.Add(gridPos + downright);
                vertices.Add(gridPos + downleft);
                vertices.Add(gridPos + upleft);

                return;
            }
            else if (moveables.Length == 1)
            {
                gridPos = m_GridSystem.IndexToPosition(moveables[0]);

                vertices.Add(gridPos + upright);
                vertices.Add(gridPos + downright);
                vertices.Add(gridPos + downleft);
                vertices.Add(gridPos + upleft);

                return;
            }

            List<float3x2> temp = new List<float3x2>();
            for (int i = 0; i < moveables.Length; i++)
            {
                gridPos = m_GridSystem.IndexToPosition(moveables[i]);

                //GridIndex
                //    right = moveables[i].GetDirection(Direction.Right),
                //    down = moveables[i].GetDirection(Direction.Down),
                //    left = moveables[i].GetDirection(Direction.Left),
                //    up = moveables[i].GetDirection(Direction.Up);

                if (m_GridSystem.TryGetDirection(moveables[i], Direction.Right, out var right) &&
                    !moveables.Contains(right))
                {
                    temp.Add(new float3x2(
                        gridPos + upright,
                        gridPos + downright
                        ));
                }

                // Down
                if (m_GridSystem.TryGetDirection(moveables[i], Direction.Right, out var down) &&
                    !moveables.Contains(down))
                {
                    temp.Add(new float3x2(
                        gridPos + downright,
                        gridPos + downleft
                        ));
                }

                if (m_GridSystem.TryGetDirection(moveables[i], Direction.Right, out var left) &&
                    !moveables.Contains(left))
                {
                    temp.Add(new float3x2(
                        gridPos + downleft,
                        gridPos + upleft
                        ));
                }

                // Up
                if (m_GridSystem.TryGetDirection(moveables[i], Direction.Right, out var up) &&
                    !moveables.Contains(up))
                {
                    temp.Add(new float3x2(
                        gridPos + upleft,
                        gridPos + upright
                        ));
                }
            }

            float3x2 current = temp[temp.Count - 1];
            temp.RemoveAt(temp.Count - 1);

            for (int i = temp.Count - 1; i >= 0; i--)
            {
                vertices.Add(current.c0);
                vertices.Add(current.c1);

                if (!FindFloat3x2(temp, current.c1, out current))
                {
                    break;
                }
            }
        }

        private static bool FindFloat3x2(List<float3x2> list, float3 next, out float3x2 found)
        {
            found = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].c0.Equals(next) || list[i].c1.Equals(next))
                {
                    found = list[i];
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        #region UI

        public void DrawUICell(Entity<IEntityData> entity)
        {
            using (m_DrawUICellMarker.Auto())
            {
                if (!entity.HasComponent<TRPGActorMoveComponent>())
                {
                    "error".ToLogError();
                    return;
                }

                if (m_IsDrawingGrids)
                {
                    ClearUICell();
                }

                TRPGActorMoveComponent move = entity.GetComponent<TRPGActorMoveComponent>();
                GetMoveablePositions(entity.Idx, ref m_GridTempMoveables);
                CalculateMoveableOutlineVertices(entity.Idx, m_GridTempMoveables, ref m_GridTempOutlines);

                m_GridOutlineRenderer.positionCount = m_GridTempOutlines.Length;
                m_GridOutlineRenderer.SetPositions(m_GridTempOutlines);

                GridComponent gridSize = entity.GetComponentReadOnly<GridComponent>();

                //for (int i = 0; i < m_GridTempMoveables.Length; i++)
                //{
                //    PlaceUICell(in gridSize, m_GridTempMoveables[i]);
                //}

#if CORESYSTEM_HDRP
                m_GridOutlineCamera = m_RenderSystem.GetProjectionCamera(
                    CoreSystemSettings.Instance.m_TRPGGridLineMaterial,
                    CoreSystemSettings.Instance.m_TRPGGridProjectionTexture);
                m_GridOutlineCamera.SetPosition(m_GridSystem.IndexToPosition(gridSize.Indices[0]));
#endif
                m_IsDrawingGrids = true;
            }
        }
        //private void PlaceUICell(in GridComponent gridSize, in GridIndex position)
        //{
        //    using (m_PlaceUICellMarker.Auto())
        //    {
        //        if (gridSize.IsMyIndex(position)) return;

        //        Entity<IEntity> entity = m_GridSystem.PlaceUICell(position);
        //    }
        //}
        public void ClearUICell()
        {
            using (m_ClearUICellMarker.Auto())
            {
                if (!m_IsDrawingGrids) return;

                //m_GridSystem.ClearUICell();

                m_GridOutlineRenderer.positionCount = 0;

#if CORESYSTEM_HDRP
                m_GridOutlineCamera.Dispose();
                m_GridOutlineCamera = null;
#endif

                m_IsDrawingGrids = false;
            }
        }

        public void DrawUIPath(in FixedList4096Bytes<GridIndex> path, float heightOffset = .5f)
        {
            if (m_IsDrawingPaths)
            {
                ClearUIPath();
            }

            m_GridTempPathlines.Clear();
            float3 offset = new float3(0, heightOffset, 0);

            m_GridPathlineRenderer.positionCount = path.Length;
            for (int i = 0; i < path.Length; i++)
            {
                m_GridTempPathlines.Add(m_GridSystem.IndexToPosition(path[i]) + offset);
            }
            m_GridPathlineRenderer.SetPositions(m_GridTempPathlines);

            m_IsDrawingPaths = true;
        }
        public void ClearUIPath()
        {
            if (!m_IsDrawingPaths) return;

            m_GridPathlineRenderer.positionCount = 0;

            m_IsDrawingPaths = false;
        }

        #endregion

        public ActorEventHandler MoveToCell(IEntityDataID entity, in FixedList4096Bytes<GridIndex> path, in ActorMoveEvent ev)
        {
#if DEBUG_MODE
            if (!entity.HasComponent<TRPGActorMoveComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have {nameof(TRPGActorMoveComponent)}." +
                    $"Maybe didn\'t added {nameof(TRPGActorMoveProvider)} in {nameof(ActorControllerAttribute)}?");
                return ActorEventHandler.Empty;
            }
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have {nameof(NavAgentAttribute)} attribute.");
                return ActorEventHandler.Empty;
            }
#endif
            ActorEventHandler handler = m_NavMeshSystem.MoveTo(Entity<IEntity>.GetEntity(entity.Idx), path, ev);

            ref TurnPlayerComponent turnPlayer = ref entity.GetComponent<TurnPlayerComponent>();
            int requireAp = path.Length;

            turnPlayer.ActionPoint -= requireAp;
            $"{turnPlayer.ActionPoint} : {requireAp}".ToLog();

            return handler;
        }
        public ActorEventHandler MoveToCell(IEntityDataID entity, GridIndex position)
        {
#if DEBUG_MODE
            if (!entity.HasComponent<TRPGActorMoveComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have {nameof(TRPGActorMoveComponent)}." +
                    $"Maybe didn\'t added {nameof(TRPGActorMoveProvider)} in {nameof(ActorControllerAttribute)}?");
                return ActorEventHandler.Empty;
            }
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.Name}) doesn\'t have {nameof(NavAgentAttribute)} attribute.");
                return ActorEventHandler.Empty;
            }
#endif
            TRPGActorMoveComponent move = entity.GetComponent<TRPGActorMoveComponent>();
            FixedList4096Bytes<GridIndex> path = new FixedList4096Bytes<GridIndex>();
            if (!m_GridSystem.GetPath(entity.Idx, position, ref path))
            {
                "path error not found".ToLogError();
                return ActorEventHandler.Empty;
            }

            ActorEventHandler handler = m_NavMeshSystem.MoveTo(Entity<IEntity>.GetEntity(entity.Idx),
                path, new ActorMoveEvent(Entity<IEntityData>.GetEntityWithoutCheck(entity.Idx), 1));

            ref TurnPlayerComponent turnPlayer = ref entity.GetComponent<TurnPlayerComponent>();
            int requireAp = path.Length;

            turnPlayer.ActionPoint -= requireAp - 1;

            return handler;
        }
    }
}