using UnityEditor;

using Syadeu.Mono;
using UnityEngine;

namespace Syadeu
{
    [CustomEditor(typeof(PrefabList))]
    public class PrefabListEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorUtils.StringHeader("Prefab List");
            EditorUtils.SectorLine();

            EditorGUI.BeginChangeCheck();

            var objSettings = serializedObject.FindProperty("m_ObjectSettings");
            EditorGUILayout.PropertyField(objSettings);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            //base.OnInspectorGUI();
        }
    }
}
