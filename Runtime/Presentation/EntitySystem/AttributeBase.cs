using Newtonsoft.Json;
using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    /// <inheritdoc cref="IAttribute"/>
    public abstract class AttributeBase : ObjectBase, IAttribute
    {
        [JsonIgnore] public IObject Parent { get; internal set; }

        public override sealed string ToString() => Name;
        public override sealed object Clone() => base.Clone();
    }

    //[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    //public sealed class RequireEntityAttribute : Attribute
    //{
    //    internal Type m_Type;
    //    public RequireEntityAttribute(Type type)
    //    {
    //        m_Type = type;
    //    }
    //}
}
