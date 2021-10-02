using Syadeu.Mono;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [SubSystem(typeof(GridSystem))]
    public sealed class TRPGGridSystem : PresentationSystemEntity<TRPGGridSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        private LineRenderer m_GridBoundsLineRenderer;
        private NativeList<GridPosition> m_GridBoundsTempMoveables;
        private NativeList<Vector3> m_GridBoundsTempOutlines;
        private bool[] m_GridBoundsMouseOver;

        private bool m_IsDrawingGrids = false;

        public bool IsDrawingUIGrid => m_IsDrawingGrids;

        private InputSystem m_InputSystem;
        private GridSystem m_GridSystem;

        protected override PresentationResult OnInitialize()
        {
            m_GridBoundsLineRenderer = CreateGameObject("Line Renderer").AddComponent<LineRenderer>();
            m_GridBoundsLineRenderer.numCornerVertices = 1;
            m_GridBoundsLineRenderer.numCapVertices = 1;
            m_GridBoundsLineRenderer.alignment = LineAlignment.View;
            m_GridBoundsLineRenderer.textureMode = LineTextureMode.Tile;

            m_GridBoundsLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            m_GridBoundsLineRenderer.startWidth = CoreSystemSettings.Instance.m_TRPGGridLineWidth;
            m_GridBoundsLineRenderer.endWidth = CoreSystemSettings.Instance.m_TRPGGridLineWidth;
            m_GridBoundsLineRenderer.material = CoreSystemSettings.Instance.m_TRPGGridLineMaterial;

            m_GridBoundsLineRenderer.loop = true;
            m_GridBoundsLineRenderer.positionCount = 0;

            m_GridBoundsTempMoveables = new NativeList<GridPosition>(512, Allocator.Persistent);
            m_GridBoundsTempOutlines = new NativeList<Vector3>(512, Allocator.Persistent);

            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GridSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_GridBoundsTempMoveables.Dispose();
            m_GridBoundsTempOutlines.Dispose();
        }

        #region Binds

        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }
        private void Bind(GridSystem other)
        {
            m_GridSystem = other;
        }

        #endregion

        public void DrawUICell(EntityData<IEntityData> entity)
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
            move.GetMoveablePositions(ref m_GridBoundsTempMoveables);
            move.CalculateMoveableOutlineVertices(m_GridBoundsTempMoveables, ref m_GridBoundsTempOutlines);
            
            m_GridBoundsLineRenderer.positionCount = m_GridBoundsTempOutlines.Length;
            m_GridBoundsLineRenderer.SetPositions(m_GridBoundsTempOutlines);
            m_GridBoundsMouseOver = new bool[m_GridBoundsLineRenderer.positionCount];

            GridSizeComponent gridSize = entity.GetComponent<GridSizeComponent>();

            for (int i = 0; i < m_GridBoundsTempMoveables.Length; i++)
            {
                PlaceUICell(in gridSize, m_GridBoundsTempMoveables[i]);
            }

            m_IsDrawingGrids = true;
        }
        private void PlaceUICell(in GridSizeComponent gridSize, in GridPosition position)
        {
            if (gridSize.IsMyIndex(position.index)) return;

            m_GridSystem.PlaceUICell(position);
        }
        public void ClearUICell()
        {
            if (!m_IsDrawingGrids) return;

            m_GridSystem.ClearUICell();

            m_GridBoundsLineRenderer.positionCount = 0;

            m_IsDrawingGrids = false;
        }
    }
}