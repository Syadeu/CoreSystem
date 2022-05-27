using GraphProcessor;
using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Graphs
{
    [Preserve, CustomJsonConverter]
    internal sealed class StickyNoteJsonConverter : JsonConverter
    {
        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.InheritsFrom<StickyNote>(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            StickyNote note = (StickyNote)value;

            writer.WriteStartObject();
            {
                writer.WritePropertyName("position");
                serializer.Serialize(writer, note.position);

                writer.WriteProperty("title", note.title);
                writer.WriteProperty("content", note.content);
            }
            writer.WriteEndObject();
        }
    }
}
