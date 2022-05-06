using GraphProcessor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Collections.Converters;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Graphs
{
    [Preserve, CustomJsonConverter]
    internal sealed class VisualGraphJsonConverter : JsonConverter
    {
        public sealed class EdgeHelper
        {
            public string 
                guid,
                inputNodeGUID, outputNodeGUID,

                inputFieldName, outputFieldName,
                inputPortIdentifier, outputPortIdentifier;

            public void Initialize(JObject edge)
            {
                const string 
                    c_Guid = "guid", 
                    c_InputNodeGuid = "inputNodeGuid", c_OutputNodeGuid = "outputNodeGuid";

                guid = edge[c_Guid].ToString();

                inputNodeGUID = edge[c_InputNodeGuid].ToString();
                if (inputNodeGUID == null) inputNodeGUID = string.Empty;
                outputNodeGUID = edge[c_OutputNodeGuid].ToString();
                if (outputNodeGUID == null) outputNodeGUID = string.Empty;

                inputFieldName = edge["inputFieldName"].ToString();
                if (inputFieldName == null) inputFieldName = string.Empty;
                outputFieldName = edge["outputFieldName"].ToString();
                if (outputFieldName == null) outputFieldName = string.Empty;
                inputPortIdentifier = edge["inputPortIdentifier"].ToString();
                if (inputPortIdentifier == null) inputPortIdentifier = string.Empty;
                outputPortIdentifier = edge["outputPortIdentifier"].ToString();
                if (outputPortIdentifier == null) outputPortIdentifier = string.Empty;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.InheritsFrom<VisualGraph>(objectType);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            JObject jo = JObject.Load(reader);
            Type type = Type.GetType(jo["type"].ToString());

            Dictionary<Guid, BaseNode> nodeHashMap = new Dictionary<Guid, BaseNode>();
            VisualGraph graph = (VisualGraph)ScriptableObject.CreateInstance(type);
            graph.nodes = jo["nodes"].ToObject<List<BaseNode>>(serializer);
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                graph.nodes[i].Initialize(graph);
                nodeHashMap.Add(Guid.Parse(graph.nodes[i].GUID), graph.nodes[i]);
            }

            JArray edges = (JArray)jo["edges"];
            EdgeHelper helper = new EdgeHelper();
            for (int i = 0; i < edges.Count; i++)
            {
                helper.Initialize((JObject)edges[i]);

                BaseNode inputNode = nodeHashMap[Guid.Parse(helper.inputNodeGUID)];
                NodePort inputPort = inputNode.GetPort(helper.inputFieldName, helper.inputPortIdentifier);

                BaseNode outputNode = nodeHashMap[Guid.Parse(helper.outputNodeGUID)];
                NodePort outputPort = outputNode.GetPort(helper.outputFieldName, helper.outputPortIdentifier);

                SerializableEdge temp = SerializableEdge.CreateNewEdge(graph, inputPort, outputPort);
                temp.GUID = helper.guid;
                temp.OnBeforeSerialize();

                graph.edges.Add(temp);
            }

            graph.groups = jo["groups"].ToObject<List<Group>>(serializer);
            graph.stackNodes = jo["stackNodes"].ToObject<List<BaseStackNode>>(serializer);
            graph.pinnedElements = jo["pinnedElements"].ToObject<List<PinnedElement>>(serializer);
            graph.exposedParameters = jo["exposedParameters"].ToObject<List<ExposedParameter>>(serializer);

            graph.stickyNotes = jo["stickyNotes"].ToObject<List<StickyNote>>(serializer);
            graph.position = jo["position"].ToObject<Vector3>(serializer);
            graph.scale = jo["scale"].ToObject<Vector3>(serializer);

            graph.Deserialize();

            return graph;
        }
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            VisualGraph graph = (VisualGraph)value;
            writer.WriteStartObject();
            {
                writer.WriteProperty("type", value.GetType().AssemblyQualifiedName);

                writer.WritePropertyName("nodes");
                serializer.Serialize(writer, graph.nodes);

                writer.WritePropertyName("edges");
                serializer.Serialize(writer, graph.edges);

                writer.WritePropertyName("groups");
                serializer.Serialize(writer, graph.groups);

                writer.WritePropertyName("stackNodes");
                serializer.Serialize(writer, graph.stackNodes);

                writer.WritePropertyName("pinnedElements");
                serializer.Serialize(writer, graph.pinnedElements);

                writer.WritePropertyName("exposedParameters");
                serializer.Serialize(writer, graph.exposedParameters);

                writer.WritePropertyName("stickyNotes");
                serializer.Serialize(writer, graph.stickyNotes);

                writer.WritePropertyName("position");
                serializer.Serialize(writer, graph.position);
                writer.WritePropertyName("scale");
                serializer.Serialize(writer, graph.scale);
            }
            writer.WriteEndObject();
        }
    }
}
