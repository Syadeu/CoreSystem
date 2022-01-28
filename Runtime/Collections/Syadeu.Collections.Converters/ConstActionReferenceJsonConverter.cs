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
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class ConstActionReferenceJsonConverter : JsonConverter<IConstActionReference>
    {
        private readonly Type[] m_ConstructorParam;

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public ConstActionReferenceJsonConverter() : base()
        {
            m_ConstructorParam = new Type[] { TypeHelper.TypeOf<Guid>.Type, TypeHelper.TypeOf<IEnumerable<object>>.Type };
        }

        public override IConstActionReference ReadJson(JsonReader reader, Type objectType, IConstActionReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            Guid guid = jo["Guid"].ToObject<Guid>();
            
            ConstructorInfo constructor = objectType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                callConvention: CallingConventions.HasThis,
                m_ConstructorParam,
                null
                );


            if (!guid.Equals(Guid.Empty))
            {
                ConstActionUtilities.TryGetWithGuid(guid, out var targetConstAction);

                JArray argsArr = (JArray)jo["Arguments"];
                object[] args = new object[argsArr.Count];
                for (int i = 0; i < argsArr.Count; i++)
                {
                    args[i] = argsArr[i].ToObject(targetConstAction.ArgumentFields[i].FieldType);
                }

                return (IConstActionReference)constructor.Invoke(new object[] { guid, args });
            }

            return (IConstActionReference)constructor.Invoke(new object[] { Guid.Empty, null });
        }

        public override void WriteJson(JsonWriter writer, IConstActionReference value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Guid");
            writer.WriteValue(value.Guid);

            writer.WritePropertyName("Arguments");
            writer.WriteStartArray();
            if (!value.IsEmpty())
            {
                for (int i = 0; i < value.Arguments.Length; i++)
                {
                    serializer.Serialize(writer, value.Arguments[i]);
                }
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
