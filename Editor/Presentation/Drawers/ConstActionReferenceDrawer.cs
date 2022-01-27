using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using SyadeuEditor.Utilities;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class ConstActionReferenceDrawer : ObjectDrawer<IConstActionReference>
    {
        private static readonly Type[] s_ConstActionTypes;
        static ConstActionReferenceDrawer()
        {
            s_ConstActionTypes = TypeHelper.GetTypes(t => !t.IsInterface && !t.IsAbstract &&
                TypeHelper.TypeOf<IConstAction>.Type.IsAssignableFrom(t));
        }

        public ConstActionReferenceDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public ConstActionReferenceDrawer(IList list, int index, Type elementType) : base(list, index, elementType)
        {
        }
        public ConstActionReferenceDrawer(object parentObject, Type declaredType, Action<IConstActionReference> setter, Func<IConstActionReference> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override IConstActionReference Draw(IConstActionReference currentValue)
        {
            Type targetType;
            Type[] generics = DeclaredType.GetGenericArguments();
            if (generics.Length > 0) targetType = generics[0];
            else targetType = null;

            var iter = s_ConstActionTypes.Where(t => t.GUID.Equals(currentValue.Guid));
            string targetName;
            Type currentActionType = null;
            if (iter.Any())
            {
                currentActionType = iter.First();
                targetName = TypeHelper.ToString(currentActionType);
            }
            else
            {
                targetName = "None";
            }

            bool clicked;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(Name, GUILayout.Width(Screen.width * .25f));

                clicked = EditorUtilities.BoxButton(targetName, ColorPalettes.PastelDreams.Mint, () =>
                {
                });
            }

            if (clicked)
            {
                DrawSelectionWindow((t) =>
                {
                    var ctor = TypeHelper.GetConstructorInfo(
                        DeclaredType, TypeHelper.TypeOf<Guid>.Type);

                    object temp;
                    if (t == null)
                    {
                        temp = ctor.Invoke(new object[] { Guid.Empty });
                    }
                    else temp = ctor.Invoke(new object[] { t.GUID });

                    Setter.Invoke((IConstActionReference)temp);

                }, targetType);
            }

            return currentValue;
        }

        static void DrawSelectionWindow(Action<Type> setter, Type targetType)
        {
            Rect rect = GUILayoutUtility.GetRect(150, 300);
            rect.position = Event.current.mousePosition;

            var sort = s_ConstActionTypes.Where(t => t.BaseType.GenericTypeArguments[0].Equals(targetType)).ToArray();

            PopupWindow.Show(rect, SelectorPopup<Type, Type>.GetWindow(
                list: sort,
                setter: setter,
                getter: (t) =>
                {
                    return t;
                },
                noneValue: null,
                other => TypeHelper.ToString(other)
                ));
        }
    }
}
