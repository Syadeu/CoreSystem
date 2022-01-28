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

using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Syadeu.Collections
{
    public static class ConstActionUtilities
    {
        private static Type[] s_Types;
        public static Type[] Types
        {
            get
            {
                if (s_Types == null)
                {
                    s_Types = TypeHelper
                        .GetTypesIter(t => !t.IsInterface && !t.IsAbstract &&
                            TypeHelper.TypeOf<IConstAction>.Type.IsAssignableFrom(t)).ToArray();
                }

                return s_Types;
            }
        }
        private static Dictionary<Type, Info> s_HashMap;
        public static Dictionary<Type, Info> HashMap
        {
            get
            {
                if (s_HashMap == null)
                {
                    s_HashMap = new Dictionary<Type, Info>(Types.Length);
                    for (int i = 0; i < Types.Length; i++)
                    {
                        s_HashMap.Add(Types[i], new Info(Types[i]));
                    }
                }

                return s_HashMap;
            }
        }

        public sealed class Info
        {
            private readonly Type m_Type;
            private readonly Type m_ReturnType;
            private readonly FieldInfo[] m_ArgumentFields;
            private readonly JsonPropertyAttribute[] m_JsonPropertyAttributes;

            public Type Type => m_Type;
            public Type ReturnType => m_ReturnType;
            public FieldInfo[] ArgumentFields => m_ArgumentFields;
            public JsonPropertyAttribute[] JsonAttributes => m_JsonPropertyAttributes;

            internal Info(Type type)
            {
                m_Type = type;
                m_ReturnType = type.BaseType.GenericTypeArguments[0];

                var iter
                    = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(t => t.GetCustomAttribute<JsonPropertyAttribute>() != null);
                if (iter.Any())
                {
                    m_ArgumentFields = iter.ToArray();
                    Array.Sort(m_ArgumentFields, comparer: new JsonPropertyComparer());

                    m_JsonPropertyAttributes = m_ArgumentFields.Select(t => t.GetCustomAttribute<JsonPropertyAttribute>()).ToArray();
                }
                else
                {
                    m_ArgumentFields = Array.Empty<FieldInfo>();
                    m_JsonPropertyAttributes = Array.Empty<JsonPropertyAttribute>();
                }
            }

            public void SetArguments(IConstAction instance, params object[] args)
            {
                for (int i = 0; i < args.Length && i < m_ArgumentFields.Length; i++)
                {
                    m_ArgumentFields[i].SetValue(instance, args[i]);
                }
            }
        }

        public static bool TryGetWithGuid(Guid guid, out Info info)
        {
            var iter = Types.Where(t => t.GUID.Equals(guid));
            if (iter.Any())
            {
                info = HashMap[iter.First()];
                return true;
            }

            info = null;
            return false;
        }
    }
}
