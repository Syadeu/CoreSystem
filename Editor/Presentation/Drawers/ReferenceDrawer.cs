using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ReferenceDrawer : ObjectDrawer<IReference>
    {
        private bool m_Open;

        public ReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }

        public ReferenceDrawer(object parentObject, Type declaredType, Action<IReference> setter, Func<IReference> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override IReference Draw(IReference currentValue)
        {
            Type targetType;
            Type[] generics = DeclaredType.GetGenericArguments();
            if (generics.Length > 0) targetType = generics[0];
            else targetType = null;

            GUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();

            ReflectionHelperEditor.DrawReferenceSelector(Name, (idx) =>
            {
                ObjectBase objBase = EntityDataList.Instance.GetObject(idx);

                object temp = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                    new object[] { objBase });

                Setter.Invoke((IReference)temp);
            }, currentValue, targetType);

            m_Open = GUILayout.Toggle(m_Open,
                        m_Open ? EditorUtils.FoldoutOpendString : EditorUtils.FoldoutClosedString
                        , EditorUtils.MiniButton, GUILayout.Width(20));

            EditorGUI.BeginDisabledGroup(!currentValue.IsValid());
            if (GUILayout.Button("C", GUILayout.Width(20)))
            {
                ObjectBase clone = (ObjectBase)currentValue.GetObject().Clone();

                clone.Hash = Hash.NewHash();
                clone.Name += "_Clone";
                EntityDataList.Instance.m_Objects.Add(clone.Hash, clone);

                object temp = TypeHelper.GetConstructorInfo(DeclaredType, TypeHelper.TypeOf<ObjectBase>.Type).Invoke(
                    new object[] { clone });
                currentValue = (IReference)temp;

                if (EntityWindow.IsOpened)
                {
                    EntityWindow.Instance.Reload();
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (m_Open)
            {
                Color color3 = Color.red;
                color3.a = .7f;

                using (new EditorUtils.BoxBlock(color3))
                {
                    if (!currentValue.IsValid())
                    {
                        EditorGUILayout.HelpBox(
                            "This reference is invalid.",
                            MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "This is shared reference. Anything made changes in this inspector view will affect to original reference directly.",
                            MessageType.Info);

                        EntityWindow.ObjectBaseDrawer.GetDrawer(currentValue.GetObject()).OnGUI();
                        //SetAttribute(m_CurrentList[i], m_AttributeDrawers[i].OnGUI());
                    }
                }
            }
            EditorGUILayout.EndVertical();

            return currentValue;
        }
    }
}
