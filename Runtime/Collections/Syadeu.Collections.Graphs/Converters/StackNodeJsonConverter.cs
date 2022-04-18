using GraphProcessor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Collections.Converters;
using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Graphs
{
    [Preserve, CustomJsonConverter]
    internal sealed class StackNodeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.InheritsFrom<BaseStackNode>(objectType);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            Type type = Type.GetType(jo["type"].ToString());
            BaseStackNode stack;
            try
            {
                stack = (BaseStackNode)Activator.CreateInstance(type);

                stack.position = JsonConvert.DeserializeObject<Vector2>(jo["position"].ToString());
                stack.title = jo["title"].ToString();
                stack.acceptDrop = jo["acceptDrop"].ToObject<bool>();
                stack.acceptNewNode = jo["acceptNewNode"].ToObject<bool>();

                stack.nodeGUIDs = new System.Collections.Generic.List<string>();
                JArray arr = (JArray)jo["nodeGuids"];
                for (int i = 0; i < arr.Count; i++)
                {
                    stack.nodeGUIDs.Add(arr[i].ToString());
                }

                arr.AutoSetFrom(type, stack);
            }
            catch (Exception)
            {
                stack = new BaseStackNode(
                    position: JsonConvert.DeserializeObject<Vector2>(jo["position"].ToString()),
                    title: jo["title"].ToString(),
                    acceptDrop: jo["acceptDrop"].ToObject<bool>(),
                    acceptNewNode: jo["acceptNewNode"].ToObject<bool>()
                    );

                stack.nodeGUIDs = new System.Collections.Generic.List<string>();
                JArray arr = (JArray)jo["nodeGuids"];
                for (int i = 0; i < arr.Count; i++)
                {
                    stack.nodeGUIDs.Add(arr[i].ToString());
                }
            }

            return stack;
        }
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            Type targetType = value.GetType();
            BaseStackNode stack = (BaseStackNode)value;

            writer.WriteStartObject();
            {
                writer.WriteProperty("type", targetType.AssemblyQualifiedName);

                writer.WritePropertyName("position");
                serializer.Serialize(writer, stack.position);

                writer.WriteProperty("title", stack.title);
                writer.WriteProperty("acceptDrop", stack.acceptDrop);
                writer.WriteProperty("acceptNewNode", stack.acceptNewNode);

                writer.WritePropertyName("nodeGuids");
                writer.WriteStartArray();
                {
                    for (int i = 0; i < stack.nodeGUIDs.Count; i++)
                    {
                        writer.WriteValue(stack.nodeGUIDs[i]);
                    }
                }
                writer.WriteEndArray();

                if (!TypeHelper.TypeOf<BaseStackNode>.Type.Equals(targetType))
                {
                    writer.AutoWriteForThisType(targetType, stack);
                }
            }
            writer.WriteEndObject();
        }
    }
}
