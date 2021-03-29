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
        private static Material s_Material;
        private static Material DefaultMaterial
        {
            get
            {
                if (s_Material == null) s_Material = AssetDatabase.GetBuiltinExtraResource<Material>(DEFAULT_MATERIAL);
                return s_Material;
            }
        }

        #region GL

        public static void GLDrawLine(in Vector3 from, in Vector3 to) => GLDrawLine(in from, in to, Color.white);
        public static void GLDrawLine(in Vector3 from, in Vector3 to, in Color color)
        {
            if (!GLIsDrawable(from) && !GLIsDrawable(to)) return;

            GL.PushMatrix();
            DefaultMaterial.SetPass(0);
            GL.Color(color);
            GL.Begin(GL.LINES);
            {
                GL.Vertex(from); GL.Vertex(to);
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawMesh(Mesh mesh, Material material = null)
        {
            if (material == null) material = DefaultMaterial;

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
        public static void GLDrawMesh(in Vector3 center, Mesh mesh, Material material = null)
        {
            if (material == null) material = DefaultMaterial;

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
                        in currentColor);

                    if (currentColor.Equals(red)) currentColor = green;
                    else if (currentColor.Equals(green)) currentColor = blue;
                    else currentColor = red;
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void GLDrawBounds(in Bounds bounds)
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

            //Material material = DefaultMaterial;
            //material.SetPass(0);

            GL.PushMatrix();
            DefaultMaterial.SetPass(0);
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
        public static void GLDrawBounds(in Bounds bounds, in Color color)
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
        public static void GLDrawWireBounds(in Bounds bounds)
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

            Material material = DefaultMaterial;
            material.SetPass(0);

            GL.PushMatrix();
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
        public static void GLDrawWireBounds(in Bounds bounds, in Color color)
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
        public static void GLDrawCube(in Vector3 position, in Vector3 size) => GLDrawBounds(new Bounds(position, size));
        public static void GLDrawWireBounds(in Vector3 position, in Vector3 size) => GLDrawWireBounds(new Bounds(position, size));
        public static void GLDrawCube(in Vector3 position, in Vector3 size, in Color color) => GLDrawBounds(new Bounds(position, size), in color);
        public static void GLDrawWireBounds(in Vector3 position, in Vector3 size, in Color color) => GLDrawWireBounds(new Bounds(position, size), in color);

        private static bool GLIsDrawable(in Vector3 worldPos)
        {
            return EditorSceneUtils.IsDrawable(EditorSceneUtils.ToScreenPosition(worldPos));
        }
        private static void GLTri(in Vector3 v0, in Vector3 v1, in Vector3 v2, in Color color)
        {
            if (GLIsDrawable(v0) || GLIsDrawable(v1) || GLIsDrawable(v2))
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2);
            }
        }
        private static void GLQuad(in Vector3 v0, in Vector3 v1, in Vector3 v2, in Vector3 v3, in Color color)
        {
            if (GLIsDrawable(v0) || GLIsDrawable(v1) || GLIsDrawable(v2) || GLIsDrawable(v3))
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2); GL.Vertex(v3);
            }
        }
        private static void GLDuo(in Vector3 v0, in Vector3 v1, in Color color)
        {
            if (GLIsDrawable(v0) || GLIsDrawable(v1))
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1);
            }
        }

        #endregion
    }
}
