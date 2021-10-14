using Syadeu.Collections;
using Syadeu.Presentation;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor.Presentation
{
    [CustomEditor(typeof(ReferenceScriptableObject))]
    public sealed class ReferenceScriptableObjectEditor : EditorEntity<ReferenceScriptableObject>
    {
        private ReferenceDrawer Drawer;

        private void OnEnable()
        {
            PropertyInfo property = GetProperty(nameof(ReferenceScriptableObject.Reference));
            Drawer = new ReferenceDrawer(target, property);

            if (!EntityDataList.IsLoaded)
            {
                EntityDataList.Instance.LoadData();
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            Drawer.OnGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}
