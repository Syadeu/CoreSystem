using GraphProcessor;
using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Graphs
{
    [Preserve, CustomJsonConverter]
    internal sealed class SerializableEdgeJsonConverter : JsonConverter
    {
        public override bool CanRead => false;
        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.InheritsFrom<SerializableEdge>(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            SerializableEdge edge = (SerializableEdge)value;

            writer.WriteStartObject();
            {
                writer.WriteProperty("guid", edge.GUID);
                writer.WriteProperty("inputNodeGuid", edge.inputNode?.GUID);
                writer.WriteProperty("outputNodeGuid", edge.outputNode?.GUID);

                writer.WriteProperty("inputFieldName", edge.inputFieldName);
                writer.WriteProperty("outputFieldName", edge.outputFieldName);

                writer.WriteProperty("inputPortId", edge.inputPort?.portData.identifier);
                writer.WriteProperty("outputPortId", edge.outputPort?.portData.identifier);
            }
            writer.WriteEndObject();
        }
    }
}
