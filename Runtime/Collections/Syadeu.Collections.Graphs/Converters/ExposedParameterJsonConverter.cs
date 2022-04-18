using GraphProcessor;
using Newtonsoft.Json;
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

        public override bool CanRead => false;
        public override bool CanWrite => base.CanWrite;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            Type targetType = value.GetType();
            ExposedParameter parameter = (ExposedParameter)value;

            writer.WriteStartObject();
            {
                writer.WriteProperty("guid", parameter.guid);
                writer.WriteProperty("name", parameter.name);

                writer.WriteProperty("input", parameter.input);

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
