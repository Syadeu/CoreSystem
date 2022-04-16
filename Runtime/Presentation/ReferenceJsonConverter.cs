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
using Syadeu.Collections;
using Syadeu.Collections.Converters;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Converters
{
    [CustomJsonConverter, Preserve]
    internal sealed class ReferenceJsonConverter : JsonConverter<IFixedReference>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override IFixedReference ReadJson(JsonReader reader, Type objectType, IFixedReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jToken = JToken.Load(reader);

            Hash hash;
            // Prev
            if (jToken.Type == JTokenType.Object)
            {
                JObject jObj = (JObject)jToken;
                hash = jObj.Value<ulong>("Hash");
            }
            else hash = jToken.Value<ulong>();

            if (objectType.GenericTypeArguments.Length > 0)
            {
                Type targetT = typeof(Reference<>).MakeGenericType(objectType.GenericTypeArguments[0]);

                return (IFixedReference)TypeHelper.GetConstructorInfo(targetT, TypeHelper.TypeOf<Hash>.Type)
                    .Invoke(new object[] { hash });
            }
            else
            {
                return new Reference(hash);
            }
        }
        public override void WriteJson(JsonWriter writer, IFixedReference value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Hash);
        }
    }
}
