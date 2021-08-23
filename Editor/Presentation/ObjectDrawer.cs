using Syadeu.Internal;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public abstract class ObjectDrawer<T> : ObjectDrawerBase
    {
        private object m_TargetObject;
        private Type m_DelaredType;
        private Action<T> m_Setter;
        private Func<T> m_Getter;

        private readonly Attribute[] m_Attributes;
        private readonly bool m_Disable;

        public override sealed object TargetObject => m_TargetObject;
        public override string Name { get; }
        public Type DelaredType => m_DelaredType;

        public Func<T> Getter => m_Getter;

        public ObjectDrawer(object parentObject, MemberInfo memberInfo)
        {
            m_TargetObject = parentObject;

            if (memberInfo is FieldInfo field)
            {
                m_DelaredType = field.FieldType;

                m_Setter = (other) => field.SetValue(m_TargetObject, other);
                m_Getter = () =>
                {
                    object value = field.GetValue(m_TargetObject);
                    return value == null ? default(T) : (T)value;
                };
            }
            else if (memberInfo is PropertyInfo property)
            {
                m_DelaredType = property.PropertyType;

                m_Setter = (other) => property.SetValue(m_TargetObject, other);
                m_Getter = () => (T)property.GetValue(m_TargetObject);
            }
            else throw new NotImplementedException();

            m_Attributes = memberInfo.GetCustomAttributes().ToArray();
            m_Disable = m_Attributes.Where((other) => other.Equals(TypeHelper.TypeOf<ReflectionSealedViewAttribute>.Type)).Any();

            Name = ReflectionHelper.SerializeMemberInfoName(memberInfo);
        }

        public override sealed void OnGUI()
        {
            foreach (var item in m_Attributes)
            {
                if (item is SpaceAttribute)
                {
                    EditorGUILayout.Space();
                }
                else if (item is TooltipAttribute tooltip)
                {
                    EditorGUILayout.HelpBox(tooltip.tooltip, MessageType.Info);
                }
                else if (item is ReflectionDescriptionAttribute description)
                {
                    EditorGUILayout.HelpBox(description.m_Description, MessageType.Info);
                }
                else if (item is HeaderAttribute header)
                {
                    EditorUtils.Line();
                    EditorUtils.StringRich(header.header, 15);
                }
            }

            EditorGUI.BeginDisabledGroup(m_Disable);
            m_Setter.Invoke(Draw(m_Getter.Invoke()));
            EditorGUI.EndDisabledGroup();
        }
        public abstract T Draw(T currentValue);
    }
}
