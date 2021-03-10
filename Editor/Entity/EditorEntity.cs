using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    /// <summary>
    /// Script Custom Editor Entity
    /// </summary>
    public abstract class EditorEntity : Editor
    {
        private const string DEFAULT_MATERIAL = "Sprites-Default.mat";

        #region GL

        public static void GLDrawLine(Vector3 from, Vector3 to)
        {
            if (!GLIsDrawable(from) && !GLIsDrawable(to)) return;

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            {
                GL.Vertex(from); GL.Vertex(to);
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawMesh(Mesh mesh, Material material = null)
        {
            if (material == null) material = AssetDatabase.GetBuiltinExtraResource<Material>(DEFAULT_MATERIAL);

            Color
                red = new Color { r = 1, a = 0.1f },
                green = new Color { g = 1, a = 0.1f },
                blue = new Color { b = 1, a = 0.1f };

            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            {
                Color currentColor = red;
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    GLTri(mesh.vertices[mesh.triangles[i]], 
                        mesh.vertices[mesh.triangles[i + 1]],
                        mesh.vertices[mesh.triangles[i + 2]],
                        currentColor);

                    if (currentColor.Equals(red)) currentColor = green;
                    else if (currentColor.Equals(green)) currentColor = blue;
                    else currentColor = red;
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawMesh(Vector3 center, Mesh mesh, Material material = null)
        {
            if (material == null) material = AssetDatabase.GetBuiltinExtraResource<Material>(DEFAULT_MATERIAL);

            Color
                red = new Color { r = 1, a = 0.1f },
                green = new Color { g = 1, a = 0.1f },
                blue = new Color { b = 1, a = 0.1f };

            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.TRIANGLES);
            {
                Color currentColor = red;
                for (int i = 0; i < mesh.triangles.Length; i += 3)
                {
                    GLTri(mesh.vertices[mesh.triangles[i]] + center, 
                        mesh.vertices[mesh.triangles[i + 1]] + center,
                        mesh.vertices[mesh.triangles[i + 2]] + center,
                        currentColor);

                    if (currentColor.Equals(red)) currentColor = green;
                    else if (currentColor.Equals(green)) currentColor = blue;
                    else currentColor = red;
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawBounds(Bounds bounds)
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
            Color
                red = new Color { r = 1, a = 0.1f },
                green = new Color { g = 1, a = 0.1f },
                blue = new Color { b = 1, a = 0.1f };

            Material material = AssetDatabase.GetBuiltinExtraResource<Material>(DEFAULT_MATERIAL);
            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            {
                GLQuad(b3, b2, b1, b0, green);// Y-
                GLQuad(b1, t1, t0, b0, red);// X-
                GLQuad(b0, t0, t3, b3, blue);// Z-
                GLQuad(b3, t3, t2, b2, red);// X+
                GLQuad(b2, t2, t1, b1, blue);// Z+
                GLQuad(t0, t1, t2, t3, green);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawBounds(Bounds bounds, Color color)
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
            color.a = .1f;

            Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            {
                GLQuad(b3, b2, b1, b0, color);// Y-
                GLQuad(b1, t1, t0, b0, color);// X-
                GLQuad(b0, t0, t3, b3, color);// Z-
                GLQuad(b3, t3, t2, b2, color);// X+
                GLQuad(b2, t2, t1, b1, color);// Z+
                GLQuad(t0, t1, t2, t3, color);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawWireBounds(Bounds bounds)
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
            Color
                red = new Color { r = 1, a = .15f },
                green = new Color { g = 1, a = .15f },
                blue = new Color { b = 1, a = .15f };

            Material material = AssetDatabase.GetBuiltinExtraResource<Material>(DEFAULT_MATERIAL);
            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            {
                GLQuad(b3, b2, b1, b0, green);// Y-
                GLQuad(b1, t1, t0, b0, red);// X-
                GLQuad(b0, t0, t3, b3, blue);// Z-
                GLQuad(b3, t3, t2, b2, red);// X+
                GLQuad(b2, t2, t1, b1, blue);// Z+
                GLQuad(t0, t1, t2, t3, green);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawWireBounds(Bounds bounds, Color color)
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
            color.a = .15f;

            Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            {
                GLQuad(b3, b2, b1, b0, color);// Y-
                GLQuad(b1, t1, t0, b0, color);// X-
                GLQuad(b0, t0, t3, b3, color);// Z-
                GLQuad(b3, t3, t2, b2, color);// X+
                GLQuad(b2, t2, t1, b1, color);// Z+
                GLQuad(t0, t1, t2, t3, color);// Y+
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawCube(Vector3 position, Vector3 size) => GLDrawBounds(new Bounds(position, size));
        public static void GLDrawWireBounds(Vector3 position, Vector3 size) => GLDrawWireBounds(new Bounds(position, size));
        public static void GLDrawCube(Vector3 position, Vector3 size, Color color) => GLDrawBounds(new Bounds(position, size), color);
        public static void GLDrawWireBounds(Vector3 position, Vector3 size, Color color) => GLDrawWireBounds(new Bounds(position, size), color);

        private static bool GLIsDrawable(Vector3 worldPos)
        {
            return EditorSceneUtils.IsDrawable(EditorSceneUtils.ToScreenPosition(worldPos));
        }
        private static void GLTri(Vector3 v0, Vector3 v1, Vector3 v2, Color color)
        {
            if (GLIsDrawable(v0) || GLIsDrawable(v1) || GLIsDrawable(v2))
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2);
            }
        }
        private static void GLQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            if (GLIsDrawable(v0) || GLIsDrawable(v1) || GLIsDrawable(v2) || GLIsDrawable(v3))
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2); GL.Vertex(v3);
            }
        }

        #endregion
    }
}
