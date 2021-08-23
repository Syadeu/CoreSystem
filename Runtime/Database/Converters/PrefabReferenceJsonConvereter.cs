using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Database.Converters
{
    [Preserve]
    internal sealed class PrefabReferenceJsonConvereter : JsonConverter<PrefabReference>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override PrefabReference ReadJson(JsonReader reader, Type objectType, PrefabReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jToken = JToken.Load(reader);
            long value;

            // Prev
            if (jToken.Type == JTokenType.Object)
            {
                JObject jObj = (JObject)jToken;
                value = jObj.Value<long>("m_Idx");
            }
            else value = jToken.Value<long>();

            return new PrefabReference(value);
        }

        public override void WriteJson(JsonWriter writer, PrefabReference value, JsonSerializer serializer)
        {
            writer.WriteValue(value.m_Idx);
        }
    }
}
