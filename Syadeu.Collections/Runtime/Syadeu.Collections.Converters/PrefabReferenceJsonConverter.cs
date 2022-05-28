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
using System.Reflection;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class PrefabReferenceJsonConverter : JsonConverter<IPrefabReference>
    {
        private readonly Type[] m_ConstructorParam;

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public PrefabReferenceJsonConverter() : base()
        {
            m_ConstructorParam = new Type[] { TypeHelper.TypeOf<long>.Type, TypeHelper.TypeOf<string>.Type };
        }

        public override IPrefabReference ReadJson(JsonReader reader, Type objectType, IPrefabReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jToken = JToken.Load(reader);
            long value;
            string subAssetName;

            if (jToken.Type == JTokenType.Object)
            {
                JObject jObj = (JObject)jToken;
                value = jObj.Value<long>("Index");
                subAssetName = jObj.Value<string>("SubAssetName");
            }
            else
            {
                value = jToken.Value<long>();
                subAssetName = string.Empty;
            }

            return (IPrefabReference)objectType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, 
                CallingConventions.HasThis, 
                m_ConstructorParam, 
                null)
                .Invoke(new object[] { value, subAssetName });
        }
        public override void WriteJson(JsonWriter writer, IPrefabReference value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Index");
            writer.WriteValue(value.Index);

            writer.WritePropertyName("SubAssetName");
            writer.WriteValue(value.SubAssetName.ToString());

            writer.WriteEndObject();
        }
    }
}
