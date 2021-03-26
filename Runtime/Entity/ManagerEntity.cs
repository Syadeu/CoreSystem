using UnityEngine;

namespace Syadeu
{
    public abstract class ManagerEntity : MonoBehaviour
    {
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

        private static Color
            whiteLine = new Color { a = 1f },
            red = new Color { r = 1, a = 0.1f },
            green = new Color { g = 1, a = 0.1f },
            blue = new Color { b = 1, a = 0.1f };

        private static Material s_DefaultMaterial;
        private static Material DefaultMaterial
        {
            get
            {
                if (s_DefaultMaterial == null)
                {
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

        protected void GLDrawLine(in Vector3 from, in Vector3 to)
        {
            DefaultMaterial.SetPass(0);

            GL.PushMatrix();
            //GL.MultMatrix(transform.localToWorldMatrix);

            GL.Begin(GL.LINES);
            {
                GL.Vertex(from); GL.Vertex(to);
            }
            GL.End();
            GL.PopMatrix();
        }
        protected void GLDrawMesh(Mesh mesh, Material material = null)
        {
            if (material == null) material = DefaultMaterial;

            Color
                red = new Color { r = 1, a = 0.1f },
                green = new Color { g = 1, a = 0.1f },
                blue = new Color { b = 1, a = 0.1f };

            material.SetPass(0);

            GL.PushMatrix();
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
        protected void GLDrawMesh(in Vector3 center, Mesh mesh, Material material = null)
        {
            if (material == null) material = DefaultMaterial;

            material.SetPass(0);

            GL.PushMatrix();
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
        protected void GLDrawPlane(in Vector3 center, in Vector2 size, in Color color, bool withOutline = false)
        {
            Vector2 half = size * .5f;

            Vector3
                b0 = new Vector3(center.x - half.x, center.y + .1f, center.z - half.y),
                b1 = new Vector3(center.x - half.x, center.y + .1f, center.z + half.y),
                b2 = new Vector3(center.x + half.x, center.y + .1f, center.z + half.y),
                b3 = new Vector3(center.x + half.x, center.y + .1f, center.z - half.y);

            DefaultMaterial.SetPass(0);

            GL.PushMatrix();
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
            GL.PopMatrix();
        }
        protected void GLDrawBounds(in Bounds bounds)
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

            Material material = DefaultMaterial;
            material.SetPass(0);

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
        protected void GLDrawBounds(in Bounds bounds, in Color color)
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

            Material material = DefaultMaterial;
            material.SetPass(0);

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
        protected void GLDrawWireBounds(in Bounds bounds)
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

            Material material = DefaultMaterial;
            material.SetPass(0);

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
        protected void GLDrawWireBounds(in Bounds bounds, in Color color)
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

            Material material = DefaultMaterial;
            material.SetPass(0);

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
        protected void GLDrawCube(in Vector3 position, in Vector3 size) => GLDrawBounds(new Bounds(position, size));
        protected void GLDrawWireBounds(in Vector3 position, in Vector3 size) => GLDrawWireBounds(new Bounds(position, size));
        protected void GLDrawCube(in Vector3 position, in Vector3 size, in Color color) => GLDrawBounds(new Bounds(position, size), in color);
        protected void GLDrawWireBounds(in Vector3 position, in Vector3 size, in Color color) => GLDrawWireBounds(new Bounds(position, size), in color);

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
    }
}
