using Syadeu;
using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Utilities
{
    [Obsolete("Use Unity Serialized -> PropertyDrawer<T>", true)]
    public abstract class ObjectDrawer<T> : ObjectDrawerBase
    {
        private object m_TargetObject;
        private Type m_DelaredType;
        private Action<T> m_Setter;
        private Func<T> m_Getter;

        private readonly Attribute[] m_FieldAttributes;
        private readonly bool m_Disable;

        public override sealed object TargetObject => m_TargetObject;
        public override string Name { get; }
        public Type DeclaredType => m_DelaredType;
        public Attribute[] Attributes => m_FieldAttributes;

        public Func<T> Getter => m_Getter;
        public Action<T> Setter => m_Setter;

        public ObjectDrawer(object parentObject, Type declaredType, Action<T> setter, Func<T> getter)
        {
            m_TargetObject = parentObject;
            m_DelaredType = declaredType;
            m_Setter = setter;
            m_Getter = getter;

            m_FieldAttributes = Array.Empty<Attribute>();
            m_Disable = false;

            Name = string.Empty;
        }
        public ObjectDrawer(IList list, int index, Type elementType) 
        {
            m_TargetObject = list;
            m_DelaredType = elementType;

            m_Setter = other => list[index] = other;
            m_Getter = () => list[index] == null ? default(T) : (T)list[index];

            m_FieldAttributes = Array.Empty<Attribute>();
            m_Disable = false;

            Name = string.Empty;
        }
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

                if (property.GetSetMethod() == null)
                {
                    m_Setter = null;
                }
                else m_Setter = (other) => property.SetValue(m_TargetObject, other);
                m_Getter = () =>
                {
                    object temp;
                    try
                    {
                        temp = property.GetValue(m_TargetObject);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        return default(T);
                    }
                    
                    return (T)temp;
                };
            }
            else throw new NotImplementedException();

            m_FieldAttributes = memberInfo.GetCustomAttributes().ToArray();
            m_Disable = memberInfo.GetCustomAttribute<ReflectionSealedViewAttribute>() != null;

            Name = ReflectionHelper.SerializeMemberInfoName(memberInfo);
        }

        public override sealed void OnGUI()
        {
            using (new EditorGUI.DisabledGroupScope(m_Disable || m_Setter == null))
            {
                try
                {
                    T obj = m_Getter.Invoke();

                    if (m_Setter == null)
                    {
                        Draw(obj);
                    }
                    else
                    {
                        T changed = Draw(obj);

                        //if (obj is IEquatable<T> equal)
                        //{
                        //    if (!equal.Equals(changed))
                            {
                                m_Setter.Invoke(changed);
                                GUI.changed = true;
                                //"1 in".ToLog();
                            }
                        //}
                        //else
                        //{
                        //    if (!obj.Equals(changed))
                        //    {
                        //        m_Setter.Invoke(changed);
                        //        GUI.changed = true;
                        //        "2 in".ToLog();
                        //    }
                        //}
                    }
                }
                catch (Exception e)
                {
                    if (ExitGUIUtility.ShouldRethrowException(e))
                    {
                        throw;
                    }
                    Debug.LogException(e);
                }
            }
        }
        public abstract T Draw(T currentValue);

        protected TAttribute GetFieldAttribute<TAttribute>() where TAttribute : Attribute
        {
            for (int i = 0; i < m_FieldAttributes.Length; i++)
            {
                if (TypeHelper.TypeOf<TAttribute>.Type.IsAssignableFrom(m_FieldAttributes[i].GetType()))
                {
                    return (TAttribute)m_FieldAttributes[i];
                }
            }

            return null;
        }
    }
}
