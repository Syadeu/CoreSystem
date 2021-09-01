using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class AttributeAcceptOnlyAttribute : Attribute
    {
        public Type[] Types;
        public AttributeAcceptOnlyAttribute(params Type[] types)
        {
            //for (int i = 0; i < types.Length; i++)
            //{
            //    if (!TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(types[i]))
            //    {
            //        CoreSystem.Logger.LogError(Channel.Entity, $"Type({types[i].Name}) is not a entity type.");
            //        throw new Exception();
            //    }
            //}
            
            Types = types;
        }
    }
}
