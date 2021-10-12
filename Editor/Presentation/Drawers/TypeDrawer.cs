﻿using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor.Presentation
{
    public sealed class TypeDrawer : ObjectDrawer<Type>
    {
        public override int FieldCount => 1;

        public TypeDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public TypeDrawer(object parentObject, Type declaredType, Action<Type> setter, Func<Type> getter) : base(parentObject, declaredType, setter, getter)
        {
        }

        public override Type Draw(Type currentValue)
        {
            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.TextField(TypeHelper.ToString(currentValue));

            EditorGUI.EndDisabledGroup();
            return currentValue;
        }
    }
}
