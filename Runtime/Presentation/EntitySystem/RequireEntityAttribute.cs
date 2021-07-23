using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequireEntityAttribute : Attribute
    {
        public Type Type;
        public RequireEntityAttribute(Type type)
        {
            if (!TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(type))
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"Type({type.Name}) is not a entity type.");
                throw new Exception();
            }
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class EntityAcceptOnlyAttribute : Attribute
    {
        public Type AttributeType;

        public EntityAcceptOnlyAttribute(Type attributeType)
        {
            if (!TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(attributeType))
            {
                CoreSystem.Logger.LogError(Channel.Entity, $"Type({attributeType.Name}) is not a attribute type.");
                throw new Exception();
            }
            AttributeType = attributeType;
        }
    }
}
