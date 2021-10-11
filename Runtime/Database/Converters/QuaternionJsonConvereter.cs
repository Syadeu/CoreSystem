using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class QuaternionJsonConvereter : JsonConverter<quaternion>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override quaternion ReadJson(JsonReader reader, Type objectType, quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JArray jo = (JArray)JToken.Load(reader);
            return new quaternion(jo[0].Value<float>(), jo[1].Value<float>(), jo[2].Value<float>(), jo[3].Value<float>());
        }

        public override void WriteJson(JsonWriter writer, quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            writer.WriteValue(value.value.x);
            writer.WriteValue(value.value.y);
            writer.WriteValue(value.value.z);
            writer.WriteValue(value.value.w);

            writer.WriteEndArray();
        }
    }
}
