// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Syadeu.Collections
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
