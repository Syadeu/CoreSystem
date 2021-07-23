using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Unity.Mathematics;

namespace Syadeu.Database.Converters
{
    internal sealed class Int3JsonConverter : JsonConverter<int3>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override int3 ReadJson(JsonReader reader, Type objectType, int3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jo = (JArray)JToken.Load(reader);
            return new int3(jo[0].Value<int>(), jo[1].Value<int>(), jo[2].Value<int>());
        }

        public override void WriteJson(JsonWriter writer, int3 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteValue(value.z);
            writer.WriteEndArray();
        }
    }
    internal sealed class Int2JsonConverter : JsonConverter<int2>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override int2 ReadJson(JsonReader reader, Type objectType, int2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jo = (JArray)JToken.Load(reader);
            return new int2(jo[0].Value<int>(), jo[1].Value<int>());
        }

        public override void WriteJson(JsonWriter writer, int2 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.y);
            writer.WriteEndArray();
        }
    }
}
