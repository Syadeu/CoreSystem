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
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class HashJsonConverter : JsonConverter<Hash>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override Hash ReadJson(JsonReader reader, Type objectType, Hash existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jo = JToken.Load(reader);
            string temp = jo.ToString();

            if (ulong.TryParse(temp, out ulong hash)) return new Hash(hash);
            else return Hash.Empty;
        }

        public override void WriteJson(JsonWriter writer, Hash value, JsonSerializer serializer)
        {
            ulong hash = value;
            writer.WriteValue(hash);
        }
    }

    [CustomJsonConverter, Preserve]
    internal sealed class InputActionConverter : JsonConverter<InputAction>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override InputAction ReadJson(JsonReader reader, Type objectType, InputAction existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            JObject jo = (JObject)token;

            string name = jo["name"].ToString();
            InputActionType type = (InputActionType)jo["type"].Value<int>();
            JToken 
                interactions = jo["interactions"],
                processors = jo["processors"],
                expectedControlType = jo["expectedControlType"];

            InputAction action = new InputAction(
                name: name,
                type: type,
                interactions: interactions == null ? null : interactions.ToString(),
                processors: processors == null ? null : processors.ToString(),
                expectedControlType: expectedControlType == null ? null : expectedControlType.ToString());

            JArray array = (JArray)jo["bindings"];
            for (int i = 0; i < array.Count; i++)
            {
                JObject element = (JObject)array[i];
                string path = element["path"].ToString();

                JToken
                    elementInteraction = element["interactions"],
                    elementProcessors = element["processors"],
                    elementGroups = element["groups"];

                action.AddBinding(path, 
                    interactions: elementInteraction == null ? null : elementInteraction.ToString(),
                    processors: elementProcessors == null ? null : elementProcessors.ToString(),
                    groups: elementGroups == null ? null : elementGroups.ToString()
                    );
            }

            return action;
        }
        public override void WriteJson(JsonWriter writer, InputAction value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            {
                writer.WriteProperty("name", value?.name);
                writer.WriteProperty("type", (int)value?.type);

                writer.WritePropertyName("bindings");
                writer.WriteStartArray();
                for (int i = 0; i < value?.bindings.Count; i++)
                {
                    InputBinding binding = value.bindings[i];
                    writer.WriteStartObject();

                    writer.WriteProperty("path", binding.path);
                    if (!binding.effectiveInteractions.IsNullOrEmpty())
                    {
                        writer.WriteProperty("interactions", binding.effectiveInteractions);
                    }
                    if (!binding.effectiveProcessors.IsNullOrEmpty())
                    {
                        writer.WriteProperty("processors", binding.effectiveProcessors);
                    }
                    if (!binding.groups.IsNullOrEmpty())
                    {
                        writer.WriteProperty("groups", binding.groups);
                    }

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                if (!(value?.interactions).IsNullOrEmpty())
                {
                    writer.WriteProperty("interactions", value?.interactions);
                }
                
                if (!(value?.processors).IsNullOrEmpty())
                {
                    writer.WriteProperty("processors", value?.processors);
                }
                
                if (!(value?.expectedControlType).IsNullOrEmpty())
                {
                    writer.WriteProperty("expectedControlType", value?.expectedControlType);
                }
            }
            writer.WriteEndObject();
        }
    }
}
