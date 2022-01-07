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

using Syadeu.Presentation.Render;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Grid
{
    internal sealed class WorldGridGLModule : PresentationSystemModule<WorldGridSystem>
    {
        private RenderSystem m_RenderSystem;

        private bool m_DrawGrid;

        protected override void OnInitialize()
        {
            m_DrawGrid = true;

            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
        }

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;

            m_RenderSystem.OnRender += M_RenderSystem_OnRender;
        }

        private void M_RenderSystem_OnRender(UnityEngine.Rendering.ScriptableRenderContext arg1, Camera arg2)
        {
            if (!m_DrawGrid) return;

            using (Shapes.Draw.Command(arg2))
            {
                DrawGridGL(System.Grid, .05f);
                DrawOcc(arg2);
            }
            //GL.PushMatrix();
            //float3x3 rotmat = new float3x3(quaternion.identity);
            //float4x4 mat = new float4x4(rotmat, float3.zero);
            //GL.MultMatrix(mat);

            //GridExtensions.DefaultMaterial.SetPass(0);
            //Color
            //    colorWhite = Color.white,
            //    colorRed = Color.red;
            //colorWhite.a = .7f; colorRed.a = .5f;
            //GL.Begin(GL.QUADS);

            //GL.Color(colorWhite);
            //DrawGridGL(System.Grid, .05f);

            //GL.Color(colorRed);
            ////int[] gridEntities = m_GridEntities.Keys.ToArray();
            ////var gridEntities = m_GridEntities.GetKeyArray(AllocatorManager.Temp);
            ////m_MainGrid.DrawOccupiedCells(gridEntities);
            ////gridEntities.Dispose();

            //GL.End();
            //GL.PopMatrix();
        }

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

            var xTemp = new float3(thickness * .5f, 0, 0);
            var zTemp = new float3(0, 0, thickness * .5f);
            //$"{minPos} :: {maxPos}".ToLog();
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
                    //$"{p1}, {p2}".ToLog();
                    //GL.Vertex(p1 - zTemp); GL.Vertex(p2 - zTemp);
                    //GL.Vertex(p2 + zTemp); GL.Vertex(p1 + zTemp);

                    //GL.Vertex(p3 - xTemp); GL.Vertex(p4 - xTemp);
                    //GL.Vertex(p4 + xTemp); GL.Vertex(p3 + xTemp);
                }
            }
        }
        void DrawOcc(Camera cam)
        {
            System.CompleteJobs();

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
                Shapes.Draw.Text(target, camForward, nameSum, 3.5f, Color.red);
            }

            arr.Dispose();
        }

        protected override void OnShutDown()
        {
            m_RenderSystem.OnRender -= M_RenderSystem_OnRender;
        }
        protected override void OnDispose()
        {
            m_RenderSystem = null;
        }
    }
}
