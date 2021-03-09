using UnityEngine;

namespace SyadeuEditor
{
    public abstract class EditorGUIEntity
    {
        public static void DestroyImmediate(UnityEngine.Object obj) => UnityEngine.Object.DestroyImmediate(obj);
        public static void GLDrawBounds(Bounds bounds) => EditorEntity.GLDrawBounds(bounds);
        public static void GLDrawBounds(Bounds bounds, Color color) => EditorEntity.GLDrawBounds(bounds, color);
        public static void GLDrawCube(Vector3 position, Vector3 size) => EditorEntity.GLDrawBounds(new Bounds(position, size));
        public static void GLDrawCube(Vector3 position, Vector3 size, Color color) => EditorEntity.GLDrawBounds(new Bounds(position, size), color);
    }
}
