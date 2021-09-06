using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Mono;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class PrefabReferenceDrawer : ObjectDrawer<IPrefabReference>
    {
        private readonly ConstructorInfo m_Constructor;
        private bool m_Open = false;

        SerializedObject prefabSerialized = null;

        public PrefabReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<long>.Type);
        }
        public PrefabReferenceDrawer(object parentObject, Type declaredType, Action<IPrefabReference> setter, Func<IPrefabReference> getter) : base(parentObject, declaredType, setter, getter)
        {
            m_Constructor = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<long>.Type);
        }

        public override IPrefabReference Draw(IPrefabReference currentValue)
        {
            GUILayout.BeginHorizontal();
            ReflectionHelperEditor.DrawPrefabReference(Name,
                (idx) =>
                {
                    IPrefabReference prefab = (IPrefabReference)m_Constructor.Invoke(new object[] { idx });

                    Setter.Invoke(prefab);
                },
                currentValue);

            EditorGUI.BeginDisabledGroup(!currentValue.IsValid());
            EditorGUI.BeginChangeCheck();
            m_Open = GUILayout.Toggle(m_Open,
                        m_Open ? EditorUtils.FoldoutOpendString : EditorUtils.FoldoutClosedString
                        , EditorUtils.MiniButton, GUILayout.Width(20));
            if (EditorGUI.EndChangeCheck())
            {
                if (m_Open)
                {
                    prefabSerialized = new SerializedObject(currentValue.GetEditorAsset());
                }
                else
                {
                    prefabSerialized.ApplyModifiedProperties();
                    prefabSerialized = null;
                }
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            if (m_Open)
            {
                EditorGUI.indentLevel++;

                EditorUtils.BoxBlock box = new EditorUtils.BoxBlock(Color.black);

                PropertyInfo info = TypeHelper.TypeOf<SerializedObject>.Type.GetProperty("inspectorMode",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                info.SetValue(prefabSerialized, InspectorMode.Normal, null);
                SerializedProperty iter = prefabSerialized.GetIterator();
                bool enterChilderen = true;
                while (iter.NextVisible(enterChilderen))
                {
                    enterChilderen = false;
                    EditorGUILayout.PropertyField(iter, true);
                }

                box.Dispose();

                EditorGUI.indentLevel--;
            }

            return currentValue;
        }
    }
}
