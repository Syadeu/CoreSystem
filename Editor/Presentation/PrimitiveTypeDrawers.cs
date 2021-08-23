using System;
using System.Reflection;
using UnityEditor;

namespace SyadeuEditor.Presentation
{
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

    public sealed class ArrayDrawer : ObjectDrawer<Array>
    {
        private Type m_ElementType;

        public ArrayDrawer(object parentObject, MemberInfo memberInfo) : base(parentObject, memberInfo)
        {
            Type declaredType = GetDeclaredType(memberInfo);
            m_ElementType = declaredType.GenericTypeArguments[0];

        }
        public override Array Draw(Array currentValue)
        {
            throw new NotImplementedException();
        }
    }
}
