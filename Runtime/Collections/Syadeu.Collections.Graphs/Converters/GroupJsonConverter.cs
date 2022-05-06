using GraphProcessor;
using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Graphs
{
    [Preserve, CustomJsonConverter]
    internal sealed class GroupJsonConverter : JsonConverter
    {
        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.InheritsFrom<Group>(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            Group group = (Group)value;

            writer.WriteStartObject();
            {
                writer.WriteProperty("title", group.title);
                
                writer.WritePropertyName("color");
                serializer.Serialize(writer, group.color);

                writer.WritePropertyName("position");
                serializer.Serialize(writer, group.position);

                writer.WritePropertyName("size");
                serializer.Serialize(writer, group.size);
            }
            writer.WriteEndObject();
        }
    }
}
