using GraphProcessor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Collections.Converters;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Graphs
{
    [Preserve, CustomJsonConverter]
    internal sealed class ExposedParameterJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return TypeHelper.InheritsFrom<ExposedParameter>(objectType);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            Type type = Type.GetType(jo["type"].ToString());

            ExposedParameter parameter = (ExposedParameter)Activator.CreateInstance(type);

            parameter.guid = jo["guid"].ToString();
            parameter.name = jo["name"].ToString();

            parameter.input = jo["input"].ToObject<bool>();

            Type settingsType = Type.GetType(jo["settingsType"].ToString());
            parameter.settings = (ExposedParameter.Settings)jo["settings"].ToObject(settingsType, serializer);

            jo["settings"].AutoSetFrom(type, parameter);

            return parameter;
        }
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            Type targetType = value.GetType();
            ExposedParameter parameter = (ExposedParameter)value;

            writer.WriteStartObject();
            {
                writer.WriteProperty("type", targetType.AssemblyQualifiedName);

                writer.WriteProperty("guid", parameter.guid);
                writer.WriteProperty("name", parameter.name);

                writer.WriteProperty("input", parameter.input);

                writer.WriteProperty("settingsType", parameter.settings.GetType().AssemblyQualifiedName);
                writer.WritePropertyName("settings");
                serializer.Serialize(writer, parameter.settings);

                if (!TypeHelper.TypeOf<ExposedParameter>.Type.Equals(targetType))
                {
                    writer.AutoWriteForThisType(targetType, value);
                }
            }
            writer.WriteEndObject();
        }
    }
}
