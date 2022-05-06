using GraphProcessor;
using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Graphs
{
    [Preserve, CustomJsonConverter]
    internal sealed class SerializableTypeJsonConverter : JsonConverter<SerializableType>
    {
        public override bool CanRead => false;
        public override bool CanWrite => base.CanWrite;

        public override SerializableType ReadJson(JsonReader reader, Type objectType, SerializableType existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override void WriteJson(JsonWriter writer, SerializableType value, Newtonsoft.Json.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            {
                writer.WriteProperty("serializedType", value.serializedType);
            }
            writer.WriteEndObject();
        }
    }
}
