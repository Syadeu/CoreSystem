#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
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
    [SubSystem(typeof(DefaultPresentationGroup), typeof(GridSystem))]
    public sealed class TRPGGridSystem : PresentationSystemEntity<TRPGGridSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        private LineRenderer 
            m_GridOutlineRenderer, m_GridPathlineRenderer;

        private NativeList<GridPosition> m_GridTempMoveables;
        private NativeList<Vector3> 
            m_GridTempOutlines, m_GridTempPathlines;

        //private ComputeBuffer m_GridOutlineBuffer;
        //private Mesh m_OutlineMesh;

        private bool 
            m_IsDrawingGrids = false,
            m_IsDrawingPaths = false;

        private Unity.Profiling.ProfilerMarker
            m_DrawUICellMarker = new Unity.Profiling.ProfilerMarker($"{nameof(TRPGGridSystem)}.{nameof(DrawUICell)}"),
            m_PlaceUICellMarker = new Unity.Profiling.ProfilerMarker($"{nameof(TRPGGridSystem)}.{nameof(PlaceUICell)}"),
            m_ClearUICellMarker = new Unity.Profiling.ProfilerMarker($"{nameof(TRPGGridSystem)}.{nameof(ClearUICell)}");

        public bool IsDrawingUIGrid => m_IsDrawingGrids;
        public bool ISDrawingUIPath => m_IsDrawingPaths;

        private InputSystem m_InputSystem;
        private GridSystem m_GridSystem;

        protected override PresentationResult OnInitialize()
        {
            //m_GridOutlineBuffer = new ComputeBuffer(128, 12, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);

            //m_OutlineMesh = new Mesh();

            {
                m_GridOutlineRenderer = CreateGameObject("Grid Outline Renderer", true).AddComponent<LineRenderer>();
                m_GridOutlineRenderer.numCornerVertices = 1;
                m_GridOutlineRenderer.numCapVertices = 1;
                m_GridOutlineRenderer.alignment = LineAlignment.View;
                m_GridOutlineRenderer.textureMode = LineTextureMode.Tile;

                m_GridOutlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                m_GridOutlineRenderer.startWidth = CoreSystemSettings.Instance.m_TRPGGridLineWidth;
                m_GridOutlineRenderer.endWidth = CoreSystemSettings.Instance.m_TRPGGridLineWidth;
                m_GridOutlineRenderer.material = CoreSystemSettings.Instance.m_TRPGGridLineMaterial;

                m_GridOutlineRenderer.loop = true;
                m_GridOutlineRenderer.positionCount = 0;
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

            m_GridTempMoveables = new NativeList<GridPosition>(512, Allocator.Persistent);
            m_GridTempOutlines = new NativeList<Vector3>(512, Allocator.Persistent);
            m_GridTempPathlines = new NativeList<Vector3>(512, Allocator.Persistent);

            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GridSystem>(Bind);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            //m_GridOutlineBuffer.Release();

            m_GridTempMoveables.Dispose();
            m_GridTempOutlines.Dispose();
            m_GridTempPathlines.Dispose();

            m_InputSystem = null;
            m_GridSystem = null;
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

        //private bool m_DrawMesh = false;

        //protected override PresentationResult AfterPresentation()
        //{
        //    if (m_DrawMesh)
        //    {
        //        Graphics.DrawMesh(m_OutlineMesh, Matrix4x4.identity, CoreSystemSettings.Instance.m_TRPGGridLineMaterial, 0);
        //    }

        //    return base.AfterPresentation();
        //}

        public void DrawUICell(EntityData<IEntityData> entity)
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
                move.GetMoveablePositions(ref m_GridTempMoveables, out int count);
                move.CalculateMoveableOutlineVertices(m_GridTempMoveables, ref m_GridTempOutlines, count);

                //Graphics.drawmesh

                m_GridOutlineRenderer.positionCount = m_GridTempOutlines.Length;
                m_GridOutlineRenderer.SetPositions(m_GridTempOutlines);
                //m_GridOutlineRenderer.Simplify(.5f);

                //var buffer = m_GridOutlineBuffer.BeginWrite<float3>(0, m_GridTempOutlines.Length);
                //for (int i = 0; i < m_GridTempOutlines.Length; i++)
                //{
                //    buffer[i] = m_GridTempOutlines[i];
                //}
                //m_GridOutlineBuffer.EndWrite<float3>(m_GridTempOutlines.Length);

                //m_OutlineMesh.SetVertices(m_GridTempOutlines.AsArray());
                //m_OutlineMesh.SetIndices()
                //m_DrawMesh = true;

                GridSizeComponent gridSize = entity.GetComponent<GridSizeComponent>();

                for (int i = 0; i < m_GridTempMoveables.Length; i++)
                {
                    PlaceUICell(in gridSize, m_GridTempMoveables[i]);
                }

                m_IsDrawingGrids = true;
            }
        }
        private void PlaceUICell(in GridSizeComponent gridSize, in GridPosition position)
        {
            using (m_PlaceUICellMarker.Auto())
            {
                if (gridSize.IsMyIndex(position.index)) return;

                Entity<IEntity> entity = m_GridSystem.PlaceUICell(position);
            }
        }
        public void ClearUICell()
        {
            using (m_ClearUICellMarker.Auto())
            {
                if (!m_IsDrawingGrids) return;

                m_GridSystem.ClearUICell();

                m_GridOutlineRenderer.positionCount = 0;

                m_IsDrawingGrids = false;
            }
        }

        public void DrawUIPath(in GridPath64 path, float heightOffset = .5f)
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
                m_GridTempPathlines.Add(m_GridSystem.IndexToPosition(path[i].index) + offset);
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
    }
}