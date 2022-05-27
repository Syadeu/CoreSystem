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
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve, CustomJsonConverter]
    internal sealed class RectJsonConverter : JsonConverter<Rect>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jo = JArray.Load(reader);

            float
                x = jo[0].Value<float>(),
                y = jo[1].Value<float>(),
                w = jo[2].Value<float>(),
                h = jo[3].Value<float>();

            return new Rect(x, y, w, h);
        }

        public override void WriteJson(JsonWriter writer, Rect value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            {
                writer.WriteValue(value.x);
                writer.WriteValue(value.y);
                writer.WriteValue(value.width);
                writer.WriteValue(value.height);
            }
            writer.WriteEndArray();
        }
    }
}
