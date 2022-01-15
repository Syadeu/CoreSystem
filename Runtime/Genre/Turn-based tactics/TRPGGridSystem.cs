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
using Syadeu.Mono;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
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
        private EventSystem m_EventSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGSelectionSystem m_SelectionSystem;

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
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);

            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGSelectionSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            Destroy(m_GridOutlineRenderer.gameObject);
            Destroy(m_GridPathlineRenderer.gameObject);

            m_RenderSystem.OnRenderShapes -= OnRenderShapesHandler;

            m_EventSystem.RemoveEvent<OnShortcutStateChangedEvent>(OnShortcutStateChangedEventHandler);
            m_EventSystem.RemoveEvent<OnGridCellCursorOverrapEvent>(OnGridCellCursorOverrapEventHandler);
        }
        protected override void OnDispose()
        {
            m_GridTempMoveables.Dispose();
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

            if (!m_GridTempMoveables.Contains(ev.Index))
            {
                ClearUIPath();
                return;
            }

            DrawUIPath(grid.Indices[0], ev.Index);
        }
        private void TRPGGridCellUIPressedEventHandler(OnGridCellPreseedEvent ev)
        {
            if (!m_GridTempMoveables.Contains(ev.Index))
            {
                return;
            }

            MoveToCell(m_TurnTableSystem.CurrentTurn.Idx, ev.Index);
        }

        private bool m_DrawMesh = false;
        private List<GridIndex> m_DrawIndices = new List<GridIndex>();
        private float m_PathlineDrawOffset = 0;

        private void OnRenderShapesHandler(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera arg2)
        {
            TRPGSettings settings = TRPGSettings.Instance;

            Shapes.Draw.Push();
            Shapes.Draw.ZOffsetFactor = -1;

            if (!m_IsDrawingGrids && m_SelectionSystem.CurrentSelection.IsValid() &&
                m_SelectionSystem.CurrentSelection.HasComponent<GridComponent>())
            {
                //GridComponent gridcom = m_SelectionSystem.CurrentSelection.GetComponentReadOnly<GridComponent>();
                float3 selectionPos = m_SelectionSystem.CurrentSelection.transform.position;
                NativeArray<GridIndex> observeIndices = m_GridSystem.GetObserverIndices(AllocatorManager.Temp);

                using (Shapes.Draw.GradientFillScope())
                {
                    Shapes.Draw.GradientFill = Shapes.GradientFill.Radial(
                        selectionPos, 25,
                        //colorInner: new Color32(0xFF, 0x79, 0x79, 0xFF),
                        colorInner: settings.m_DetectionTileColorStart,
                        colorOuter: settings.m_DetectionTileColorEnd,
                        space: Shapes.FillSpace.World
                        );

                    for (int i = 0; i < observeIndices.Length; i++)
                    {
                        if (m_GridSystem.IsObserveIndexOfOnly(observeIndices[i], m_SelectionSystem.CurrentSelection.Idx))
                        {
                            continue;
                        }

                        float3 pos = m_GridSystem.IndexToPosition(observeIndices[i]);
                        Shapes.Draw.RectangleBorder(
                            pos: pos,
                            normal: Vector3.up,
                            size: (float2)m_GridSystem.CellSize,
                            pivot: Shapes.RectPivot.Center,
                            thickness: .03f
                        );
                    }
                }

                //Shapes.Draw.Texture(,,)
            }

            using (Shapes.Draw.ColorScope)
            using (Shapes.Draw.DashedScope())
            {
                Shapes.Draw.DashStyle = Shapes.DashStyle.FixedDashCount(
                   Shapes.DashType.Angled, 1, .25f, Shapes.DashSnapping.EndToEnd);

                Shapes.Draw.Color = settings.m_MovableTileColor;

                for (int i = 0; i < m_GridTempMoveables.Length; i++)
                {
                    var pos = m_GridSystem.IndexToPosition(m_GridTempMoveables[i]);

                    Shapes.Draw.RectangleBorder(
                        pos: pos,
                        normal: Vector3.up,
                        size: (float2)m_GridSystem.CellSize,
                        pivot: Shapes.RectPivot.Center,
                        thickness: .03f
                    );
                }
            }

            using (Shapes.Draw.ColorScope)
            {
                Shapes.Draw.Color = settings.m_MovableOutlineColor;

                if (m_ShapesOutlinePath.Count > 0)
                {
                    //Shapes.Draw.
                    Shapes.Draw.Polyline(m_ShapesOutlinePath, true, thickness: .03f);
                }
            }

            if (m_ShapesPathline.Count > 0)
            {
                m_PathlineDrawOffset += Time.deltaTime;

                using (Shapes.Draw.ColorScope)
                using (Shapes.Draw.DashedScope())
                {
                    Shapes.Draw.Color = settings.m_PathlineColor;

                    Shapes.Draw.LineGeometry = Shapes.LineGeometry.Billboard;
                    Shapes.Draw.DashStyle = Shapes.DashStyle.MeterDashes(
                        type: Shapes.DashType.Basic, 
                        size: .75f,
                        spacing: .75f, 
                        snap: Shapes.DashSnapping.Off,
                        offset: m_PathlineDrawOffset);
                    for (int i = 0; i + 1 < m_ShapesPathline.Count - 1; i++)
                    {
                        Shapes.Draw.Line(
                            m_ShapesPathline[i].point, m_ShapesPathline[i + 1].point,
                            thickness: .1f);
                    }

                    float3
                        prevPos = m_ShapesPathline[m_ShapesPathline.Count - 2].point,
                        lastPos = m_ShapesPathline[m_ShapesPathline.Count - 1].point,
                        dir = math.normalize(lastPos - prevPos);
                    lastPos -= dir * .65f;

                    Shapes.Draw.Line(
                        prevPos, lastPos,
                        thickness: .1f);

                    Shapes.Draw.Push();

                    Shapes.Draw.Color = settings.m_PathlineOverlayColor;

                    Shapes.Draw.BlendMode = Shapes.ShapesBlendMode.ColorDodge;
                    Shapes.Draw.ZTest = UnityEngine.Rendering.CompareFunction.Greater;
                    Shapes.Draw.StencilComp = UnityEngine.Rendering.CompareFunction.Always;
                    Shapes.Draw.StencilOpPass = UnityEngine.Rendering.StencilOp.Keep;
                    Shapes.Draw.StencilRefID = 0;
                    Shapes.Draw.StencilReadMask = 255;
                    Shapes.Draw.StencilWriteMask = 255;

                    for (int i = 0; i + 1 < m_ShapesPathline.Count - 1; i++)
                    {
                        Shapes.Draw.Line(
                            m_ShapesPathline[i].point, m_ShapesPathline[i + 1].point, 
                            thickness: .1f);
                    }

                    Shapes.Draw.Line(
                        prevPos, lastPos,
                        thickness: .1f);

                    Shapes.Draw.Pop();
                }

                using (Shapes.Draw.ColorScope)
                using (Shapes.Draw.DashedScope())
                {
                    Shapes.Draw.Color = settings.m_PathlineEndTipColor;

                    Shapes.Draw.LineGeometry = Shapes.LineGeometry.Flat2D;
                    Shapes.Draw.DashStyle = Shapes.DashStyle.FixedDashCount(
                        type: Shapes.DashType.Basic,
                        snap: Shapes.DashSnapping.Tiling,
                        count: 2,
                        offset: m_PathlineDrawOffset
                        );
                    Shapes.Draw.Ring(m_ShapesPathline.LastPoint.point, Vector3.up,
                        radius: .65f,
                        thickness: .08f);
                }
            }

            Shapes.Draw.Pop();
        }

        #endregion

        protected override PresentationResult OnStartPresentation()
        {
            m_RenderSystem.OnRenderShapes += OnRenderShapesHandler;

            return base.OnStartPresentation();
        }
        protected override PresentationResult AfterPresentation()
        {
            if (m_DrawMesh)
            {
                Graphics.DrawMesh(m_OutlineMesh, Matrix4x4.identity, CoreSystemSettings.Instance.m_TRPGGridLineMaterial, 0);
            }

            return base.AfterPresentation();
        }

        #endregion

        #region Math

        public void GetMoveablePositions(in InstanceID entity,
            ref NativeList<GridIndex> gridPositions)
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

                gridPositions.Add(item);
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
        public void CalculateOutlineVertices(
            in InstanceID entity,
            NativeArray<GridIndex> moveables,
            ref NativeList<Vector3> vertices)
        {
            var gridsize = entity.GetComponent<GridComponent>();
            float half = m_GridSystem.CellSize * .5f;

            float3
                upleft = new float3(-half, 0, half),
                upright = new float3(half, 0, half),
                downleft = new float3(-half, 0, -half),
                downright = new float3(half, 0, -half);

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
                //$"{gridPos}".ToLog();
                if (!m_GridSystem.TryGetDirection(moveables[i], Direction.Right, out var right) ||
                    !moveables.Contains(right))
                {
                    temp.Add(new float3x2(
                        gridPos + upright,
                        gridPos + downright
                        ));
                }

                // Down
                if (!m_GridSystem.TryGetDirection(moveables[i], Direction.Forward, out var down) ||
                    !moveables.Contains(down))
                {
                    temp.Add(new float3x2(
                        gridPos + downright,
                        gridPos + downleft
                        ));
                }

                if (!m_GridSystem.TryGetDirection(moveables[i], Direction.Left, out var left) ||
                    !moveables.Contains(left))
                {
                    temp.Add(new float3x2(
                        gridPos + downleft,
                        gridPos + upleft
                        ));
                }

                // Up
                if (!m_GridSystem.TryGetDirection(moveables[i], Direction.Backward, out var up) ||
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

            do
            {
                vertices.Add(current.c0);
            } while (FindFloat3x2(temp, current.c1, out current));

            //vertices.Add(current.c1);
            //for (int i = temp.Count - 1; i >= 0; i--)
            //{
            //    vertices.Add(current.c0);
            //    vertices.Add(current.c1);

            //    if (!FindFloat3x2(temp, current.c1, out current))
            //    {
            //        break;
            //    }
            //}
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

        #endregion

        #region UI

        private Shapes.PolylinePath 
            m_ShapesOutlinePath = new Shapes.PolylinePath(),
            m_ShapesPathline = new Shapes.PolylinePath();

        private void DrawUICell(Entity<IEntityData> entity)
        {
            using (m_DrawUICellMarker.Auto())
            {
                if (m_IsDrawingGrids)
                {
                    ClearUICell();
                }

                GetMoveablePositions(entity.Idx, ref m_GridTempMoveables);
                CalculateOutlineVertices(entity.Idx, m_GridTempMoveables, ref m_GridTempOutlines);

                m_ShapesOutlinePath.ClearAllPoints();
                for (int i = 0; i < m_GridTempOutlines.Length; i++)
                {
                    m_ShapesOutlinePath.AddPoint(m_GridTempOutlines[i]);
                }

                //m_GridOutlineRenderer.positionCount = m_GridTempOutlines.Length;
                //m_GridOutlineRenderer.SetPositions(m_GridTempOutlines);

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

        //private void PlaceUICell(in GridComponent gridSize, in GridIndex position)
        //{
        //    using (m_PlaceUICellMarker.Auto())
        //    {
        //        if (gridSize.IsMyIndex(position)) return;

        //        Entity<IEntity> entity = m_GridSystem.PlaceUICell(position);
        //    }
        //}
        private void ClearUICell()
        {
            using (m_ClearUICellMarker.Auto())
            {
                if (!m_IsDrawingGrids) return;

                //m_GridSystem.ClearUICell();
                m_GridTempMoveables.Clear();
                m_GridTempOutlines.Clear();

                m_ShapesOutlinePath.ClearAllPoints();
                m_GridOutlineRenderer.positionCount = 0;

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

            m_GridPathlineRenderer.positionCount = path.Length;
            for (int i = 0; i < path.Length; i++)
            {
                m_GridTempPathlines.Add(m_GridSystem.IndexToPosition(path[i]) + offset);
            }
            m_GridPathlineRenderer.SetPositions(m_GridTempPathlines);

            m_IsDrawingPaths = true;
        }
        private void DrawUIPath(in GridIndex from, in GridIndex to)
        {
            if (m_IsDrawingPaths)
            {
                m_ShapesPathline.ClearAllPoints();
                m_GridPathlineRenderer.positionCount = 0;
            }

            FixedList4096Bytes<GridIndex> foundPath = new FixedList4096Bytes<GridIndex>();
            if (!m_GridSystem.GetPath(in from, in to, ref foundPath, out _)) return;

            for (int i = 0; i < foundPath.Length; i++)
            {
                m_ShapesPathline.AddPoint(m_GridSystem.IndexToPosition(foundPath[i]));
            }

            m_IsDrawingPaths = true;
        }
        private void ClearUIPath()
        {
            if (!m_IsDrawingPaths) return;

            m_ShapesPathline.ClearAllPoints();
            m_GridPathlineRenderer.positionCount = 0;
            m_PathlineDrawOffset = 0;

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