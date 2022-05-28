using Syadeu.Collections.Editor;
using UnityEngine;

namespace SyadeuEditor
{
    public abstract class EditorGUIEntity
    {
        public static void DestroyImmediate(Object obj) => Object.DestroyImmediate(obj);
        public static void GLDrawBounds(Bounds bounds) => InspectorEditor.GLDrawBounds(bounds);
        public static void GLDrawBounds(Bounds bounds, Color color) => InspectorEditor.GLDrawBounds(bounds, color);
        public static void GLDrawCube(Vector3 position, Vector3 size) => InspectorEditor.GLDrawBounds(new Bounds(position, size));
        public static void GLDrawCube(Vector3 position, Vector3 size, Color color) => InspectorEditor.GLDrawBounds(new Bounds(position, size), color);
    }
}
