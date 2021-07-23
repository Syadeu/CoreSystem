using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Unity.Mathematics;

namespace Syadeu.Database.Converters
{
    internal sealed class Float3JsonConverter : JsonConverter<float3>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override float3 ReadJson(JsonReader reader, Type objectType, float3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jo = (JArray)JToken.Load(reader);
            return new float3(jo[0].Value<float>(), jo[1].Value<float>(), jo[2].Value<float>());
        }

        public override void WriteJson(JsonWriter writer, float3 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteEndArray();
        }
    }
    internal sealed class Float2JsonConverter : JsonConverter<float2>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override float2 ReadJson(JsonReader reader, Type objectType, float2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jo = (JArray)JToken.Load(reader);
            return new float2(jo[0].Value<float>(), jo[1].Value<float>());
        }

        public override void WriteJson(JsonWriter writer, float2 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteEndArray();
        }
    }
}
