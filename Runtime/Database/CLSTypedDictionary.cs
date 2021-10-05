using Syadeu.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Syadeu.Database
{
    public sealed class CLSTypedDictionary<TKey, TValue>
    {
        public static TValue Value { get; set; }
    }
    public sealed class CLSTypedDictionary<TValue>
    {
        private static readonly Type s_GenericType = typeof(CLSTypedDictionary<,>);
        private static readonly Dictionary<Type, PropertyInfo> s_ParsedProperties = new Dictionary<Type, PropertyInfo>();

        public static TValue GetValue(Type key)
        {
            if (!s_ParsedProperties.TryGetValue(key, out PropertyInfo property))
            {
                Type generic = s_GenericType.MakeGenericType(key, TypeHelper.TypeOf<TValue>.Type);
                property = generic.GetProperty("Value", BindingFlags.Static | BindingFlags.Public);

                s_ParsedProperties.Add(key, property);
            }

            return (TValue)property.GetValue(null);
        }
        public static void SetValue(Type key, TValue value)
        {
            if (!s_ParsedProperties.TryGetValue(key, out PropertyInfo property))
            {
                Type generic = s_GenericType.MakeGenericType(key, TypeHelper.TypeOf<TValue>.Type);
                property = generic.GetProperty("Value", BindingFlags.Static | BindingFlags.Public);

                s_ParsedProperties.Add(key, property);
            }

            property.SetValue(null, value);
        }
    }
}
