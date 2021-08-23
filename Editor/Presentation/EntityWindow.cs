using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public sealed class EntityWindow : EditorWindowEntity<EntityWindow>
    {
        protected override string DisplayName => "Entity Window";

        ObjectBaseDrawer[] ObjectBaseDrawers;
        protected override void OnEnable()
        {
            ObjectBaseDrawers = new ObjectBaseDrawer[EntityDataList.Instance.m_Objects.Count];

            var temp = EntityDataList.Instance.m_Objects.Values.ToArray();
            for (int i = 0; i < temp.Length; i++)
            {
                ObjectBaseDrawers[i] = new ObjectBaseDrawer(temp[i]);
            }

            base.OnEnable();
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("test");
            for (int i = 0; i < ObjectBaseDrawers.Length; i++)
            {
                ObjectBaseDrawers[i].OnGUI();
            }
        }

        public sealed class ObjectBaseDrawer : ObjectDrawerBase
        {
            private readonly ObjectBase m_TargetObject;
            private Type m_Type;
            private ObsoleteAttribute m_Obsolete;

            private readonly MemberInfo[] m_Members;
            private readonly ObjectDrawerBase[] m_ObjectDrawers;

            public override object TargetObject => m_TargetObject;
            public override string Name => m_TargetObject.Name;

            public ObjectBaseDrawer(ObjectBase objectBase)
            {
                m_TargetObject = objectBase;
                m_Type = objectBase.GetType();
                m_Obsolete = m_Type.GetCustomAttribute<ObsoleteAttribute>();

                m_Members = ReflectionHelper.GetSerializeMemberInfos(m_Type);
                m_ObjectDrawers = new ObjectDrawerBase[m_Members.Length];
                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    m_ObjectDrawers[i] = ToDrawer(m_TargetObject, m_Members[i]);
                }
            }
            public override void OnGUI()
            {
                EditorGUILayout.LabelField(Name);
                for (int i = 0; i < m_ObjectDrawers.Length; i++)
                {
                    if (m_ObjectDrawers[i] == null) continue;

                    try
                    {
                        m_ObjectDrawers[i].OnGUI();
                    }
                    catch (Exception)
                    {
                        EditorGUILayout.LabelField($"Error at {m_ObjectDrawers[i].Name}");
                    }
                }
                EditorUtils.Line();
            }

            private static ObjectDrawerBase ToDrawer(object parentObject, MemberInfo memberInfo)
            {
                Type declaredType = GetDeclaredType(memberInfo);

                #region Primitive Types
                if (declaredType.IsEnum)
                {
                    return new EnumDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<int>.Type))
                {
                    return new IntDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<bool>.Type))
                {
                    return new BoolenDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<float>.Type))
                {
                    return new FloatDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<double>.Type))
                {
                    return new DoubleDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<long>.Type))
                {
                    return new LongDrawer(parentObject, memberInfo);
                }
                else if (declaredType.Equals(TypeHelper.TypeOf<string>.Type))
                {
                    return new StringDrawer(parentObject, memberInfo);
                }
                #endregion

                #region Unity Types
                if (declaredType.Equals(TypeHelper.TypeOf<Vector3>.Type))
                {
                    return new Vector3Drawer(parentObject, memberInfo);
                }
                if (declaredType.Equals(TypeHelper.TypeOf<float3>.Type))
                {
                    return new Float3Drawer(parentObject, memberInfo);
                }
                #endregion

                return null;
            }
        }
    }

    public abstract class ObjectDrawerBase : IDisposable
    {
        public abstract object TargetObject { get; }
        public abstract string Name { get; }

        public abstract void OnGUI();
        public virtual void Dispose() { }

        public static Type GetDeclaredType(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo field)
            {
                return field.FieldType;
            }
            else if (memberInfo is PropertyInfo property)
            {
                return property.PropertyType;
            }
            return null;
        }
    }
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

    #region Primitive Type Drawers
    public sealed class EnumDrawer : ObjectDrawer<Enum>
    {
        public EnumDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Enum Draw(Enum currentValue)
        {
            if (DelaredType.GetCustomAttribute<FlagsAttribute>() != null)
            {
                return EditorGUILayout.EnumFlagsField(Name, currentValue);
            }
            return EditorGUILayout.EnumPopup(Name, currentValue);
        }
    }
    public sealed class IntDrawer : ObjectDrawer<int>
    {
        public IntDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override int Draw(int currentValue)
        {
            return EditorGUILayout.IntField(Name, currentValue);
        }
    }
    public sealed class BoolenDrawer : ObjectDrawer<bool>
    {
        public BoolenDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override bool Draw(bool currentValue)
        {
            return EditorGUILayout.Toggle(Name, currentValue);
        }
    }
    public sealed class FloatDrawer : ObjectDrawer<float>
    {
        public FloatDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override float Draw(float currentValue)
        {
            return EditorGUILayout.FloatField(Name, currentValue);
        }
    }
    public sealed class DoubleDrawer : ObjectDrawer<double>
    {
        public DoubleDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override double Draw(double currentValue)
        {
            return EditorGUILayout.DoubleField(Name, currentValue);
        }
    }
    public sealed class LongDrawer : ObjectDrawer<long>
    {
        public LongDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override long Draw(long currentValue)
        {
            return EditorGUILayout.LongField(Name, currentValue);
        }
    }
    public sealed class StringDrawer : ObjectDrawer<string>
    {
        public StringDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override string Draw(string currentValue)
        {
            return EditorGUILayout.TextField(Name, currentValue);
        }
    }
    #endregion

    public sealed class Float3Drawer : ObjectDrawer<float3>
    {
        public Float3Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override float3 Draw(float3 currentValue)
        {
            return EditorGUILayout.Vector3Field(Name, currentValue);
        }
    }
    public sealed class Vector3Drawer : ObjectDrawer<Vector3>
    {
        public Vector3Drawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
        }
        public override Vector3 Draw(Vector3 currentValue)
        {
            return EditorGUILayout.Vector3Field(Name, currentValue);
        }
    }
}
