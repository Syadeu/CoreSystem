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
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Syadeu.Collections.Converters
{
    public static class JsonConverterExtensions
    {
        public static void WriteProperty<T>(this JsonWriter t, string name, T value)
        {
            t.WritePropertyName(name);
            t.WriteValue(value);
        }

        /// <summary>
        /// <paramref name="from"/> 다음 부터 자동
        /// </summary>
        /// <param name="from"></param>
        /// <param name="objectType"></param>
        /// <param name="obj"></param>
        public static void AutoSetFrom(this JToken from, Type objectType, object obj)
        {
            JToken current = from.Next;
            while (current != null)
            {
                JProperty property = (JProperty)current;
                FieldInfo field = objectType.GetField(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (field == null)
                {
                    Debug.LogError($"{property.Name} ?? deleted");
                    continue;
                }

                field.SetValue(obj, property.Value.ToObject(field.FieldType));

                current = current.Next;
            }
        }
        public static void AutoWriteForThisType(this JsonWriter wr, Type objectType, object value)
        {
            var fieldIter = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(t => t.GetCustomAttribute<JsonPropertyAttribute>() != null);
            FieldInfo[] fields = Array.Empty<FieldInfo>();
            if (fieldIter.Any())
            {
                fields = fieldIter.ToArray();
                Array.Sort(fields, comparer: new JsonPropertyComparer());
            }

            for (int i = 0; i < fields.Length; i++)
            {
                JsonPropertyAttribute property = fields[i].GetCustomAttribute<JsonPropertyAttribute>();

                wr.WritePropertyName(property.PropertyName);
                wr.WriteValue(fields[i].GetValue(value));
            }
        }
    }
}
