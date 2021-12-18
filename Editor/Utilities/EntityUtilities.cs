using Syadeu.Collections;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SyadeuEditor
{
    public static class EntityUtilities
    {
        public static TAttribute[] GetAttributesFor<TAttribute, TEntity>(this EntityDataList other)
            where TAttribute : AttributeBase
            where TEntity : EntityDataBase
        {
            var entityAccepts = TypeHelper.TypeOf<TEntity>.Type.GetCustomAttribute<EntityAcceptOnlyAttribute>();
            if (entityAccepts != null && 
                (entityAccepts.AttributeTypes == null || entityAccepts.AttributeTypes.Length == 0))
            {
                return Array.Empty<TAttribute>();
            }
            var attAccepts = TypeHelper.TypeOf<TAttribute>.Type.GetCustomAttribute<AttributeAcceptOnlyAttribute>();
            if (attAccepts != null &&
                (attAccepts.Types == null || attAccepts.Types.Length == 0))
            {
                return Array.Empty<TAttribute>();
            }

            IEnumerable<ObjectBase> iter = other.GetData(temp => temp is TAttribute);
            if (entityAccepts != null)
            {
                iter = iter.Where(temp =>
                {
                    for (int i = 0; i < entityAccepts.AttributeTypes.Length; i++)
                    {
                        if (entityAccepts.AttributeTypes[i].IsAssignableFrom(TypeHelper.TypeOf<TAttribute>.Type))
                        {
                            return true;
                        }
                    }
                    return false;
                });
            }

            iter = iter.Where(temp =>
            {
                for (int i = 0; i < attAccepts.Types.Length; i++)
                {
                    if (attAccepts.Types[i].IsAssignableFrom(TypeHelper.TypeOf<TEntity>.Type))
                    {
                        return true;
                    }
                }
                return false;
            });

            return iter.Select(temp => (TAttribute)temp).ToArray();
        }
        public static AttributeBase[] GetAttributesFor(this EntityDataList other, Type attType, Type entityType)
        {
            var entityAccepts = entityType.GetCustomAttribute<EntityAcceptOnlyAttribute>();
            if (entityAccepts != null &&
                (entityAccepts.AttributeTypes == null || entityAccepts.AttributeTypes.Length == 0))
            {
                return Array.Empty<AttributeBase>();
            }
            var attAccepts = attType.GetCustomAttribute<AttributeAcceptOnlyAttribute>();
            if (attAccepts != null &&
                (attAccepts.Types == null || attAccepts.Types.Length == 0))
            {
                return Array.Empty<AttributeBase>();
            }

            IEnumerable<ObjectBase> iter = other.GetData(temp => temp is AttributeBase);
            if (entityAccepts != null)
            {
                iter = iter.Where(temp =>
                {
                    for (int i = 0; i < entityAccepts.AttributeTypes.Length; i++)
                    {
                        if (entityAccepts.AttributeTypes[i].IsAssignableFrom(attType))
                        {
                            return true;
                        }
                    }
                    return false;
                });
            }

            if (attAccepts != null)
            {
                iter = iter.Where(temp =>
                {
                    for (int i = 0; i < attAccepts.Types.Length; i++)
                    {
                        if (attAccepts.Types[i].IsAssignableFrom(entityType))
                        {
                            return true;
                        }
                    }
                    return false;
                });
            }

            return iter.Select(temp => (AttributeBase)temp).ToArray();
        }
    }
}
