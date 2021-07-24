using Syadeu.Internal;
using System;

namespace Syadeu.Presentation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class AttributeAcceptOnlyAttribute : Attribute
    {
        public Type[] Types;
        public AttributeAcceptOnlyAttribute(params Type[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (!TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(types[i]))
                {
                    CoreSystem.Logger.LogError(Channel.Entity, $"Type({types[i].Name}) is not a entity type.");
                    throw new Exception();
                }
            }
            
            Types = types;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class EntityAcceptOnlyAttribute : Attribute
    {
        public Type[] AttributeTypes;

        public EntityAcceptOnlyAttribute(params Type[] attributeTypes)
        {
            for (int i = 0; i < attributeTypes.Length; i++)
            {
                if (!TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(attributeTypes[i]))
                {
                    CoreSystem.Logger.LogError(Channel.Entity, $"Type({attributeTypes[i].Name}) is not a attribute type.");
                    throw new Exception();
                }
            }
            
            AttributeTypes = attributeTypes;
        }
    }
}
