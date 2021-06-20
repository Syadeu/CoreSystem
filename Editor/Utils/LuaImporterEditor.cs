
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace SyadeuEditor
{
    [CustomEditor(typeof(LuaImporter))]
    public class LuaImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            var colorShift = new GUIContent("Script");
            var prop = serializedObject.FindProperty("m_Script");
            EditorGUILayout.PropertyField(prop, colorShift);
            base.ApplyRevertGUI();
        }
    }
}
