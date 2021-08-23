using System;
using System.Reflection;

namespace SyadeuEditor.Presentation
{
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
}
