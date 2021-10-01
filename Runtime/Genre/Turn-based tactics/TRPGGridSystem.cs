using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Unity.Collections;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGGridSystem : PresentationSystemEntity<TRPGGridSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private LineRenderer m_GridBoundsLineRenderer;
        private NativeList<GridPosition> m_GridBoundsTempMoveables;
        private NativeList<Vector3> m_GridBoundsTempOutlines;
        private bool[] m_GridBoundsMouseOver;

        private InputSystem m_InputSystem;

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

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            m_GridBoundsTempMoveables.Dispose();
            m_GridBoundsTempOutlines.Dispose();
        }

        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }

        private EntityData<IEntityData> m_DrawingEntityTarget;
        private bool m_IsDrawingGrids = false;

        protected override PresentationResult OnPresentationAsync()
        {
            return base.OnPresentationAsync();
        }

        public void DrawMoveableGridBounds(EntityData<IEntityData> entity)
        {
            if (!entity.HasComponent<TRPGActorMoveComponent>())
            {
                "error".ToLogError();
                return;
            }

            TRPGActorMoveComponent move = entity.GetComponent<TRPGActorMoveComponent>();
            move.GetMoveablePositions(ref m_GridBoundsTempMoveables);
            move.CalculateMoveableOutlineVertices(m_GridBoundsTempMoveables, ref m_GridBoundsTempOutlines);
            
            m_GridBoundsLineRenderer.positionCount = m_GridBoundsTempOutlines.Length;
            m_GridBoundsLineRenderer.SetPositions(m_GridBoundsTempOutlines);
            m_GridBoundsMouseOver = new bool[m_GridBoundsLineRenderer.positionCount];

            m_DrawingEntityTarget = entity;
            m_IsDrawingGrids = true;
        }
    }
}