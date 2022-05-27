using Syadeu.Collections;
using Syadeu.Presentation;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor.Presentation
{
    //[CustomEditor(typeof(ReferenceScriptableObject))]
    //[System.Obsolete("Use Unity Serialized -> PropertyDrawer<T>", true)]
    //public sealed class ReferenceScriptableObjectEditor : InspectorEditor<ReferenceScriptableObject>
    //{
    //    private ReferenceDrawer Drawer;

    //    private void OnEnable()
    //    {
    //        PropertyInfo property = GetProperty(nameof(ReferenceScriptableObject.Reference));
    //        Drawer = new ReferenceDrawer(target, property);

    //        if (!EntityDataList.IsLoaded)
    //        {
    //            EntityDataList.Instance.LoadData();
    //        }
    //    }

    //    protected override void OnInspectorGUIContents()
    //    {
    //        EditorGUI.BeginChangeCheck();
    //        Drawer.OnGUI();
    //        if (EditorGUI.EndChangeCheck())
    //        {
    //            EditorUtility.SetDirty(target);
    //        }
    //    }
    //}
}
