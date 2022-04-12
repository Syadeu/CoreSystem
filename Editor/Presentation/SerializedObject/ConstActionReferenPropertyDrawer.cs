using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Collections;
using UnityEngine;
using System;
using System.ComponentModel;

namespace SyadeuEditor.Presentation
{
    //[CustomPropertyDrawer(typeof(ConstActionReference<>), true)]
    //internal sealed class ConstActionReferenPropertyDrawer : PropertyDrawer<IConstActionReference>
    //{
    //    private Type m_TargetType;

    //    protected override void OnInitialize(SerializedProperty property)
    //    {
    //        Type[] generics = fieldInfo.FieldType.GetGenericArguments();
    //        if (generics.Length > 0) m_TargetType = generics[0];
    //        else m_TargetType = null;


    //    }
    //    protected override void BeforePropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
    //    {
    //        base.BeforePropertyGUI(ref rect, property, label);
    //    }
    //    protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
    //    {
    //        //base.OnPropertyGUI(ref rect, property, label);

    //        IConstActionReference currentValue = property.GetTargetObject
    //        string targetName;
    //        Type currentActionType = null;
    //        DescriptionAttribute description = null;
    //        if (currentValue != null && !currentValue.IsEmpty())
    //        {
    //            var iter = ConstActionUtilities.Types.Where(t => t.GUID.Equals(currentValue.Guid));
    //            if (iter.Any())
    //            {
    //                currentActionType = iter.First();
    //                targetName = TypeHelper.ToString(currentActionType);
    //                description = currentActionType.GetCustomAttribute<DescriptionAttribute>();
    //            }
    //            else
    //            {
    //                targetName = "None";
    //            }
    //        }
    //        else
    //        {
    //            targetName = "None";
    //        }
    //    }
    //}
}
