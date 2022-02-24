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
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Grid;
using Syadeu.Presentation.Render;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGGridShapesModule : PresentationSystemModule<TRPGGridSystem>
    {
#if CORESYSTEM_SHAPES
        private Shapes.PolylinePath m_WarningOutlinePath = new Shapes.PolylinePath();
        private NativeList<GridIndex> m_WarningOutlineLocations;
        private NativeList<float3> m_WarningOutlineVertices;

        private float m_PathlineDrawOffset = 0;

        private bool m_DrawWarningTiles = false;

        private WorldGridSystem m_GridSystem;
        private RenderSystem m_RenderSystem;
        private EventSystem m_EventSystem;

        private TRPGTurnTableSystem m_TurnTableSystem;
        private TRPGSelectionSystem m_SelectionSystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            m_WarningOutlineLocations = new NativeList<GridIndex>(512, AllocatorManager.Persistent);
            m_WarningOutlineVertices = new NativeList<float3>(512, AllocatorManager.Persistent);

            RequestSystem<DefaultPresentationGroup, WorldGridSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);

            RequestSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>(Bind);
            RequestSystem<TRPGIngameSystemGroup, TRPGSelectionSystem>(Bind);
        }
        protected override void OnShutDown()
        {
            m_RenderSystem.OnRenderShapes -= OnRenderShapesHandler;

            m_EventSystem.RemoveEvent<TRPGSelectionChangedEvent>(TRPGSelectionChangedEventHandler);
        }
        protected override void OnDispose()
        {
            m_WarningOutlineLocations.Dispose();
            m_WarningOutlineVertices.Dispose();

            m_GridSystem = null;
            m_RenderSystem = null;
            m_EventSystem = null;

            m_TurnTableSystem = null;
            m_SelectionSystem = null;
        }

        #region Binds

        private void Bind(WorldGridSystem other)
        {
            m_GridSystem = other;
        }
        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<TRPGSelectionChangedEvent>(TRPGSelectionChangedEventHandler);
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

        #endregion

        protected override void OnStartPresentation()
        {
            m_RenderSystem.OnRenderShapes += OnRenderShapesHandler;
        }

        #region EventHandlers

        private void TRPGSelectionChangedEventHandler(TRPGSelectionChangedEvent ev)
        {
            m_DrawWarningTiles = !ev.Entity.IsEmpty() && ev.Entity.HasComponent<GridComponent>();

            m_WarningOutlinePath.ClearAllPoints();
            m_WarningOutlineLocations.Clear();

            foreach (var index in m_GridSystem.GetObserverIndices())
            {
                if (m_GridSystem.IsObserveIndexOfOnly(index, ev.Entity.Idx))
                {
                    continue;
                }

                m_WarningOutlineLocations.Add(index);
            }

            m_GridSystem.GetOutcoastVertices(m_WarningOutlineLocations, ref m_WarningOutlineVertices);
        }

        #endregion

        private void OnRenderShapesHandler(UnityEngine.Rendering.ScriptableRenderContext arg1, Camera arg2)
        {
            TRPGSettings settings = TRPGSettings.Instance;

            Shapes.Draw.Push();
            Shapes.Draw.ZOffsetFactor = -1;

            if (m_DrawWarningTiles)
            {
                DrawWarningTiles(settings);
            }
            
            DrawMoveableTiles(settings);
            DrawMoveableOutline(settings);
            DrawMoveableOutlineWall(settings);
            DrawCoverable(settings);

            DrawPathline(settings);

            Shapes.Draw.Pop();
        }

        private void DrawWarningTiles(TRPGSettings settings)
        {
            if (System.IsDrawingUIGrid)
            {
                return;
            }
            else if (
                m_SelectionSystem.CurrentSelection.IsEmpty() ||
                !m_SelectionSystem.CurrentSelection.hasTransform)
            {
                m_DrawWarningTiles = false;
                return;
            }
            else if (m_WarningOutlineVertices.Length == 0)
            {
                return;
            }

            float3 selectionPos = m_SelectionSystem.CurrentSelection.transform.position;

            using (Shapes.Draw.Scope)
            using (Shapes.Draw.GradientFillScope())
            {
                // Not supported line...
                Shapes.Draw.GradientFill = Shapes.GradientFill.Radial(
                    selectionPos, 25,
                    colorInner: settings.m_DetectionTileColorStart,
                    colorOuter: settings.m_DetectionTileColorEnd,
                    space: Shapes.FillSpace.World
                    );
                Shapes.Draw.LineGeometry = Shapes.LineGeometry.Billboard;
                Shapes.Draw.Color = settings.m_DetectionTileColorStart;

                float cellHalf = m_GridSystem.CellSize * .5f;
                float3 a, b,
                    dir,
                    center,
                    normal;

                for (int i = 0; i + 1 < m_WarningOutlineVertices.Length; i++)
                {
                    a = m_WarningOutlineVertices[i];
                    b = m_WarningOutlineVertices[i + 1];
                    
                    Shapes.Draw.Line(
                        start: a,
                        end: b,
                        thickness: .03f
                        );

                    dir = b - a;
                    //if (dir.Equals(float3.zero)) continue;

                    center = a + (dir * .5f);
                    center.y += cellHalf;
                    normal = math.cross(dir, math.up());
                    Shapes.Draw.Rectangle(
                        pos: center,
                        normal: normal,
                        size: (float2)m_GridSystem.CellSize,
                        pivot: Shapes.RectPivot.Center
                        );
                }

                a = m_WarningOutlineVertices[m_WarningOutlineVertices.Length - 1];
                b = m_WarningOutlineVertices[0];
                dir = b - a;
                center = a + (dir * .5f);
                center.y += cellHalf;
                normal = math.cross(dir, math.up());

                Shapes.Draw.Line(
                    start: a,
                    end: b,
                    thickness: .03f
                    );
                Shapes.Draw.Rectangle(
                    pos: center,
                    normal: normal,
                    size: (float2)m_GridSystem.CellSize,
                    pivot: Shapes.RectPivot.Center
                    );
            }
        }
        private void DrawMoveableTiles(TRPGSettings settings)
        {
            var m_GridTempMoveables = System.CurrentMoveableTiles;

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
                        thickness: .02f
                    );
                }
            }
        }
        private void DrawMoveableOutline(TRPGSettings settings)
        {
            var m_ShapesOutlinePath = System.CurrentMoveableOutline;
            if (m_ShapesOutlinePath.Length < 1) return;

            using (Shapes.Draw.ColorScope)
            {
                Shapes.Draw.Color = settings.m_MovableOutlineColor;
                Shapes.Draw.LineGeometry = Shapes.LineGeometry.Billboard;

                //Shapes.Draw.Polyline(m_OutlinePath, true, thickness: .03f);
                for (int i = 0; i + 1 < m_ShapesOutlinePath.Length; i++)
                {
                    Shapes.Draw.Line(
                        start: m_ShapesOutlinePath[i],
                        end: m_ShapesOutlinePath[i + 1],
                        thickness: .02f);
                }
            }
        }
        private void DrawMoveableOutlineWall(TRPGSettings settings)
        {
            var m_ShapesOutlinePath = System.CurrentMoveableOutline;
            if (m_ShapesOutlinePath.Length < 1) return;

            float cellHalf = m_GridSystem.CellSize * .5f;
            using (Shapes.Draw.GradientFillScope())
            {
                Shapes.Draw.GradientFill = Shapes.GradientFill.Linear(
                    start: settings.m_OutlineWallColorStartPos,
                    end: settings.m_OutlineWallColorEndPos,
                    colorStart: settings.m_MovableOutlineColor,
                    colorEnd: Color.clear,
                    space: Shapes.FillSpace.Local
                    );

                float3 a, b,
                    dir,
                    center,
                    normal;
                for (int i = 0; i + 1 < m_ShapesOutlinePath.Length; i++)
                {
                    a = m_ShapesOutlinePath[i];
                    b = m_ShapesOutlinePath[i + 1];
                    dir = b - a;
                    center = a + (dir * .5f);
                    center.y += cellHalf;
                    normal = math.cross(dir, math.up());

                    Shapes.Draw.Rectangle(
                        pos: center,
                        normal: normal,
                        size: (float2)m_GridSystem.CellSize,
                        pivot: Shapes.RectPivot.Center
                        );
                }

                a = m_ShapesOutlinePath[m_ShapesOutlinePath.Length - 1];
                b = m_ShapesOutlinePath[0];
                dir = b - a;
                center = a + (dir * .5f);
                center.y += cellHalf;
                normal = math.cross(dir, math.up());

                Shapes.Draw.Rectangle(
                    pos: center,
                    normal: normal,
                    size: (float2)m_GridSystem.CellSize,
                    pivot: Shapes.RectPivot.Center
                    );
            }
        }
        private void DrawCoverable(TRPGSettings settings)
        {
            if (System.CoverableLength < 1) return;

            float cellHalf = m_GridSystem.CellSize * .5f;

            using (Shapes.Draw.Scope)
            using (Shapes.Draw.GradientFillScope())
            {
                Shapes.Draw.GradientFill = Shapes.GradientFill.Linear(
                    start: settings.m_CoverableWallColorStartPos,
                    end: settings.m_CoverableWallColorEndPos,
                    colorStart: settings.m_MovableOutlineColor,
                    colorEnd: Color.clear,
                    space: Shapes.FillSpace.Local
                    );

                Direction direction;
                float3x2 line;
                float3 a, b,
                    dir,
                    center,
                    normal;
                for (int i = 0; i < System.CoverableLength; i++)
                {
                    direction = System.CoverableDirections[i];
                    line = m_GridSystem.GetLineVerticesOf(System.CoverableIndices[i], direction);

                    a = line.c0;
                    b = line.c1;
                    dir = b - a;
                    center = a + (dir * .5f);
                    center.y += cellHalf;
                    normal = math.cross(dir, math.up());

                    Shapes.Draw.Rectangle(
                        pos: center,
                        normal: normal,
                        size: (float2)m_GridSystem.CellSize,
                        pivot: Shapes.RectPivot.Center
                        );
                }
            }
        }
        private void DrawPathline(TRPGSettings settings)
        {
            var m_ShapesPathline = System.CurrentPathline;
            if (m_ShapesPathline.Length < 2)
            {
                m_PathlineDrawOffset = 0;
                return;
            }

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
                for (int i = 0; i + 1 < m_ShapesPathline.Length - 1; i++)
                {
                    Shapes.Draw.Line(
                        m_ShapesPathline[i], m_ShapesPathline[i + 1],
                        thickness: .1f);
                }

                float3
                    prevPos = m_ShapesPathline[m_ShapesPathline.Length - 2],
                    lastPos = m_ShapesPathline[m_ShapesPathline.Length - 1],
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

                for (int i = 0; i + 1 < m_ShapesPathline.Length - 1; i++)
                {
                    Shapes.Draw.Line(
                        m_ShapesPathline[i], m_ShapesPathline[i + 1],
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
                Shapes.Draw.Ring(m_ShapesPathline[m_ShapesPathline.Length - 1], Vector3.up,
                    radius: .65f,
                    thickness: .08f);

                Shapes.Draw.Color = settings.m_PathlineOverlayColor;
                Shapes.Draw.BlendMode = Shapes.ShapesBlendMode.ColorDodge;
                Shapes.Draw.ZTest = UnityEngine.Rendering.CompareFunction.Greater;

                Shapes.Draw.Ring(m_ShapesPathline[m_ShapesPathline.Length - 1], Vector3.up,
                    radius: .65f,
                    thickness: .08f);
            }
        }
#endif
    }

    public sealed class TRPGGridObjectModule : PresentationSystemModule<TRPGGridSystem>
    {
        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
        }
        protected override void OnShutDown()
        {
            m_EventSystem.RemoveEvent<OnGridLocationChangedEvent>(OnGridLocationChangedEventHandler);
        }
        protected override void OnDispose()
        {
            m_EventSystem = null;
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnGridLocationChangedEvent>(OnGridLocationChangedEventHandler);
        }

        #endregion

        private void OnGridLocationChangedEventHandler(OnGridLocationChangedEvent ev)
        {

        }
    }
}