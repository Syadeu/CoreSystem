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
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class ColorJsonConverter : JsonConverter<Color>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override Color ReadJson(JsonReader reader, Type objectType, [AllowNull] Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jo = (JArray)JToken.Load(reader);

            return new Color(jo[0].Value<float>(), jo[1].Value<float>(), jo[2].Value<float>(), jo[3].Value<float>());
        }
        public override void WriteJson(JsonWriter writer, [AllowNull] Color value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.r);
            writer.WriteValue(value.g);
            writer.WriteValue(value.b);
            writer.WriteValue(value.a);
            writer.WriteEndArray();
        }
    }
}
