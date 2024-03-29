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

using UnityEngine;

namespace Syadeu.Entities
{
    public abstract class ManagerEntity : MonoBehaviour
    {
        internal protected static Transform InstanceGroupTr { get; set; }
        protected static readonly object ManagerLock = new object();

        #region Thread Methods

        public static System.Threading.Thread MainThread { get; protected set; }
        public static System.Threading.Thread BackgroundThread { get; protected set; }

        protected static bool IsMainthread()
            => CoreSystem.IsThisMainthread();

        /// <summary>
        /// OnBackgroundUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        protected static System.Collections.Concurrent.ConcurrentQueue<CoreRoutine> OnBackgroundCustomUpdate { get; } = new System.Collections.Concurrent.ConcurrentQueue<CoreRoutine>();
        /// <summary>
        /// OnUnityUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        protected static System.Collections.Concurrent.ConcurrentQueue<CoreRoutine> OnUnityCustomUpdate { get; } = new System.Collections.Concurrent.ConcurrentQueue<CoreRoutine>();
        /// <summary>
        /// OnUnityUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        public CoreRoutine StartUnityUpdate(System.Collections.IEnumerator enumerator)
        {
            CoreRoutine routine = new CoreRoutine(this, enumerator, false, false);
            OnUnityCustomUpdate.Enqueue(routine);

            return routine;
        }
        /// <summary>
        /// OnBackgroundUpdate 보다 일찍 실행되는 커스텀 업데이트문을 넣을 수 있습니다.
        /// </summary>
        public CoreRoutine StartBackgroundUpdate(System.Collections.IEnumerator enumerator)
        {
            CoreRoutine routine = new CoreRoutine(this, enumerator, false, true);
            OnBackgroundCustomUpdate.Enqueue(routine);

            return routine;
        }
        public void StopUnityUpdate(CoreRoutine routine) => CoreSystem.RemoveUnityUpdate(routine);
        public void StopBackgroundUpdate(CoreRoutine routine) => CoreSystem.RemoveBackgroundUpdate(routine);

        #endregion

        #region GL

        private static Color
            whiteLine = new Color { a = 1f },
            red = new Color { r = 1, a = 0.1f },
            green = new Color { g = 1, a = 0.1f },
            blue = new Color { b = 1, a = 0.1f };

        private static bool s_CreateDefaultMaterialCalled = false;
        private static Material s_DefaultMaterial;
        private static Material DefaultMaterial
        {
            get
            {
                if (s_DefaultMaterial == null)
                {
                    if (!IsMainthread())
                    {
                        lock (ManagerLock)
                        {
                            if (!s_CreateDefaultMaterialCalled)
                            {
                                s_CreateDefaultMaterialCalled = true;
                                ForegroundJob job = new ForegroundJob(() =>
                                {
                                    // Unity has a built-in shader that is useful for drawing
                                    // simple colored things.
                                    Shader shader = Shader.Find("Hidden/Internal-Colored");
                                    Material temp = new Material(shader);
                                    temp.hideFlags = HideFlags.HideAndDontSave;
                                    // Turn on alpha blending
                                    temp.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                                    temp.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                                    // Turn backface culling off
                                    temp.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                                    // Turn off depth writes
                                    temp.SetInt("_ZWrite", 0);

                                    s_DefaultMaterial = temp;
                                });
                            }
                        }

                        while (s_DefaultMaterial == null)
                        {
                            StaticManagerEntity.ThreadAwaiter(10);
                        }

                        return s_DefaultMaterial;
                    }
                    // Unity has a built-in shader that is useful for drawing
                    // simple colored things.
                    Shader shader = Shader.Find("Hidden/Internal-Colored");
                    s_DefaultMaterial = new Material(shader);
                    s_DefaultMaterial.hideFlags = HideFlags.HideAndDontSave;
                    // Turn on alpha blending
                    s_DefaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    s_DefaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    // Turn backface culling off
                    s_DefaultMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    // Turn off depth writes
                    s_DefaultMaterial.SetInt("_ZWrite", 0);
                }

                return s_DefaultMaterial;
            }
        }

        protected static void GLSetMaterial(Material mat = null)
        {
            if (mat == null) DefaultMaterial.SetPass(0);
            else mat.SetPass(0);
        }
        protected static void GLDrawLine(in Vector3 from, in Vector3 to)
        {
            GL.PushMatrix();
            //GL.MultMatrix(transform.localToWorldMatrix);

            GL.Begin(GL.LINES);
            {
                GL.Vertex(from); GL.Vertex(to);
            }
            GL.End();
            GL.PopMatrix();
        }
        protected static void GLDrawMesh(Mesh mesh, Material material = null)
        {
            Color
                red = new Color { r = 1, a = 0.1f },
                green = new Color { g = 1, a = 0.1f },
                blue = new Color { b = 1, a = 0.1f };

            GL.PushMatrix();
            if (material == null) DefaultMaterial.SetPass(0);
            else material.SetPass(0);
            //GL.MultMatrix(transform.localToWorldMatrix);
            GL.Begin(GL.TRIANGLES);
            {
                Color currentColor = red;
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    GLTri(in mesh.vertices[mesh.triangles[i]],
                        in mesh.vertices[mesh.triangles[i + 1]],
                        in mesh.vertices[mesh.triangles[i + 2]],
                        in currentColor);

                    if (currentColor.Equals(red)) currentColor = green;
                    else if (currentColor.Equals(green)) currentColor = blue;
                    else currentColor = red;
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        protected static void GLDrawMesh(in Vector3 center, Mesh mesh, Material material = null)
        {
            GL.PushMatrix();
            if (material == null) DefaultMaterial.SetPass(0);
            else material.SetPass(0);
            //GL.MultMatrix(transform.localToWorldMatrix);
            GL.Begin(GL.TRIANGLES);
            {
                Color currentColor = red;
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    GLTri(mesh.vertices[mesh.triangles[i]] + center,
                        mesh.vertices[mesh.triangles[i + 1]] + center,
                        mesh.vertices[mesh.triangles[i + 2]] + center,
                        in currentColor);

                    if (currentColor.Equals(red)) currentColor = green;
                    else if (currentColor.Equals(green)) currentColor = blue;
                    else currentColor = red;
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        protected static void GLDrawPlane(in Vector3 center, in Vector2 size, in Color color, bool withOutline = false, bool autoPush = true)
        {
            Vector2 half = size * .5f;

            Vector3
                b0 = new Vector3(center.x - half.x, center.y + .1f, center.z - half.y),
                b1 = new Vector3(center.x - half.x, center.y + .1f, center.z + half.y),
                b2 = new Vector3(center.x + half.x, center.y + .1f, center.z + half.y),
                b3 = new Vector3(center.x + half.x, center.y + .1f, center.z - half.y);

            if (autoPush) GL.PushMatrix();
            GL.Begin(GL.QUADS);
            {
                GLQuad(in b0, in b1, in b2, in b3, color);
            }
            GL.End();
            if (withOutline)
            {
                GL.Begin(GL.LINES);
                {
                    GLDuo(in b0, in b1, whiteLine);
                    GLDuo(in b1, in b2, whiteLine);
                    GLDuo(in b2, in b3, whiteLine);
                    GLDuo(in b3, in b0, whiteLine);
                }
                GL.End();
            }
            if (autoPush) GL.PopMatrix();
        }
        protected static void GLDrawBounds(in Bounds bounds)
        {
            Vector3
                min = bounds.min,
                max = bounds.max;
            Vector3
                b0 = min,
                b1 = new Vector3(min.x, min.y, max.z),
                b2 = new Vector3(max.x, min.y, max.z),
                b3 = new Vector3(max.x, min.y, min.z),
                t0 = new Vector3(min.x, max.y, min.z),
                t1 = new Vector3(min.x, max.y, max.z),
                t2 = max,
                t3 = new Vector3(max.x, max.y, min.z);

            GL.PushMatrix();
            //GL.MultMatrix(transform.localToWorldMatrix);
            GL.Begin(GL.QUADS);
            {
                GLQuad(in b3, in b2, in b1, in b0, in green);// Y-
                GLQuad(in b1, in t1, in t0, in b0, in red);// X-
                GLQuad(in b0, in t0, in t3, in b3, in blue);// Z-
                GLQuad(in b3, in t3, in t2, in b2, in red);// X+
                GLQuad(in b2, in t2, in t1, in b1, in blue);// Z+
                GLQuad(in t0, in t1, in t2, in t3, in green);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        protected static void GLDrawBounds(in Bounds bounds, in Color color)
        {
            Vector3
                min = bounds.min,
                max = bounds.max;
            Vector3
                b0 = min,
                b1 = new Vector3(min.x, min.y, max.z),
                b2 = new Vector3(max.x, min.y, max.z),
                b3 = new Vector3(max.x, min.y, min.z),
                t0 = new Vector3(min.x, max.y, min.z),
                t1 = new Vector3(min.x, max.y, max.z),
                t2 = max,
                t3 = new Vector3(max.x, max.y, min.z);

            GL.PushMatrix();
            //GL.MultMatrix(transform.localToWorldMatrix);
            GL.Begin(GL.QUADS);
            {
                GLQuad(in b3, in b2, in b1, in b0, in color);// Y-
                GLQuad(in b1, in t1, in t0, in b0, in color);// X-
                GLQuad(in b0, in t0, in t3, in b3, in color);// Z-
                GLQuad(in b3, in t3, in t2, in b2, in color);// X+
                GLQuad(in b2, in t2, in t1, in b1, in color);// Z+
                GLQuad(in t0, in t1, in t2, in t3, in color);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        protected static void GLDrawWireBounds(in Bounds bounds)
        {
            Vector3
                min = bounds.min,
                max = bounds.max;
            Vector3
                b0 = min,
                b1 = new Vector3(min.x, min.y, max.z),
                b2 = new Vector3(max.x, min.y, max.z),
                b3 = new Vector3(max.x, min.y, min.z),
                t0 = new Vector3(min.x, max.y, min.z),
                t1 = new Vector3(min.x, max.y, max.z),
                t2 = max,
                t3 = new Vector3(max.x, max.y, min.z);

            GL.PushMatrix();
            //GL.MultMatrix(transform.localToWorldMatrix);
            GL.Begin(GL.LINES);
            {
                GLDuo(in b3, in b2, in green); GLDuo(in b1, in b0, in green);// Y-

                GLDuo(in b1, in t1, in green); GLDuo(in t0, in b0, in red);// X-

                GLDuo(in b0, in t0, in green); GLDuo(in t3, in b3, in red);// Z-

                GLDuo(in b3, in t3, in green); GLDuo(in t2, in b2, in red);// X+

                GLDuo(in b2, in t2, in green); GLDuo(in t1, in b1, in red);// Z+

                GLDuo(in t0, in t1, in green); GLDuo(in t2, in t3, in red);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        protected static void GLDrawWireBounds(in Bounds bounds, in Color color)
        {
            Vector3
                min = bounds.min,
                max = bounds.max;
            Vector3
                b0 = min,
                b1 = new Vector3(min.x, min.y, max.z),
                b2 = new Vector3(max.x, min.y, max.z),
                b3 = new Vector3(max.x, min.y, min.z),
                t0 = new Vector3(min.x, max.y, min.z),
                t1 = new Vector3(min.x, max.y, max.z),
                t2 = max,
                t3 = new Vector3(max.x, max.y, min.z);

            GL.PushMatrix();
            //GL.MultMatrix(transform.localToWorldMatrix);
            GL.Begin(GL.QUADS);
            {
                GLQuad(in b3, in b2, in b1, in b0, in color);// Y-
                GLQuad(in b1, in t1, in t0, in b0, in color);// X-
                GLQuad(in b0, in t0, in t3, in b3, in color);// Z-
                GLQuad(in b3, in t3, in t2, in b2, in color);// X+
                GLQuad(in b2, in t2, in t1, in b1, in color);// Z+
                GLQuad(in t0, in t1, in t2, in t3, in color);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        protected static void GLDrawCube(in Vector3 position, in Vector3 size) => GLDrawBounds(new Bounds(position, size));
        protected static void GLDrawWireBounds(in Vector3 position, in Vector3 size) => GLDrawWireBounds(new Bounds(position, size));
        protected static void GLDrawCube(in Vector3 position, in Vector3 size, in Color color) => GLDrawBounds(new Bounds(position, size), in color);
        protected static void GLDrawWireBounds(in Vector3 position, in Vector3 size, in Color color) => GLDrawWireBounds(new Bounds(position, size), in color);

        private static void GLTri(in Vector3 v0, in Vector3 v1, in Vector3 v2, in Color color)
        {
            GL.Color(color);
            GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2);
        }
        private static void GLQuad(in Vector3 v0, in Vector3 v1, in Vector3 v2, in Vector3 v3, in Color color)
        {
            GL.Color(color);
            GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2); GL.Vertex(v3);
        }
        private static void GLDuo(in Vector3 v0, in Vector3 v1, in Color color)
        {
            GL.Color(color);
            GL.Vertex(v0); GL.Vertex(v1);
        }

        #endregion

        protected Mesh CreatePlaneMesh(in Vector3 size)
        {
            Mesh mesh = new Mesh();
            mesh.hideFlags = HideFlags.HideAndDontSave;
            mesh.vertices = new Vector3[]
            {
                Vector3.zero,
                new Vector3(size.x, 0, 0),
                new Vector3(0, 0, size.z),
                new Vector3(size.x, 0, size.z)
            };
            mesh.triangles = new int[]
            {
                0, 2, 1,
                2, 3, 1
            };
            mesh.normals = new Vector3[]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.uv = new Vector2[]
            {
                Vector2.zero,
                new Vector2(1, 0),
                new Vector2(0, 1),
                Vector2.one
            };
            return mesh;
        }
        protected void MeshDraw(Mesh mesh, Material material, Vector3 pos, Quaternion rot)
        {
            //var option = new MaterialPropertyBlock();
            //option.SetColor("default", new Color { a = .1f });
            
            Graphics.DrawMesh(mesh, pos, rot, material, 0);
            //DefaultMaterial.color = temp;
        }
    }
}
