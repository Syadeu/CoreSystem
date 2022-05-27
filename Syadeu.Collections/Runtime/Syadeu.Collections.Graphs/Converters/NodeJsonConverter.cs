using GraphProcessor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Collections.Converters;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Graphs
{
    [Preserve, CustomJsonConverter]
    internal sealed class NodeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.InheritsFrom<BaseNode>(objectType);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            Type type = Type.GetType(jo["type"].ToString());

            BaseNode node = (BaseNode)Activator.CreateInstance(type);

            node.GUID = jo["guid"].ToString();
            node.position = JsonConvert.DeserializeObject<Rect>(jo["position"].ToString());
            node.nodeLock = jo["nodeLock"].ToObject<bool>();

            jo["nodeLock"].AutoSetFrom(type, node);

            return node;
        }

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            BaseNode node = (BaseNode)value;

            Type objectType = value.GetType();

            writer.WriteStartObject();
            {
                writer.WriteProperty("type", objectType.AssemblyQualifiedName);
                writer.WriteProperty("guid", node.GUID);

                writer.WritePropertyName("position");
                serializer.Serialize(writer, node.position);

                writer.WriteProperty("nodeLock", node.nodeLock);

                if (!TypeHelper.TypeOf<BaseNode>.Type.Equals(objectType))
                {
                    writer.AutoWriteForThisType(objectType, node);
                }
            }
            writer.WriteEndObject();
        }
    }
}
