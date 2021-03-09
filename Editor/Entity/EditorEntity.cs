using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    /// <summary>
    /// Script Custom Editor Entity
    /// </summary>
    public abstract class EditorEntity : Editor
    {
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

            Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            {
                Quad(b3, b2, b1, b0, green);// Y-
                Quad(b1, t1, t0, b0, red);// X-
                Quad(b0, t0, t3, b3, blue);// Z-
                Quad(b3, t3, t2, b2, red);// X+
                Quad(b2, t2, t1, b1, blue);// Z+
                Quad(t0, t1, t2, t3, green);// Y+
            }
            GL.End();
            GL.PopMatrix();

            void Quad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2); GL.Vertex(v3);
            }
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
                Quad(b3, b2, b1, b0, color);// Y-
                Quad(b1, t1, t0, b0, color);// X-
                Quad(b0, t0, t3, b3, color);// Z-
                Quad(b3, t3, t2, b2, color);// X+
                Quad(b2, t2, t1, b1, color);// Z+
                Quad(t0, t1, t2, t3, color);// Y+
            }
            GL.End();
            GL.PopMatrix();

            void Quad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color color)
            {
                GL.Color(color);
                GL.Vertex(v0); GL.Vertex(v1); GL.Vertex(v2); GL.Vertex(v3);
            }
        }
        public static void GLDrawCube(Vector3 position, Vector3 size) => GLDrawBounds(new Bounds(position, size));
        public static void GLDrawCube(Vector3 position, Vector3 size, Color color) => GLDrawBounds(new Bounds(position, size), color);
    }
}
