using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using Unity.Mathematics;

namespace Syadeu.Database.Converters
{
    internal sealed class ReferenceJsonConverter : JsonConverter<IReference>
    {
        public override bool CanWrite => false;
        public override bool CanRead => true;

        public override IReference ReadJson(JsonReader reader, Type objectType, IReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            Hash hash = jo["Hash"].ToObject<Hash>();
            if (objectType.GenericTypeArguments.Length > 0)
            {
                Type targetT = typeof(Reference<>).MakeGenericType(objectType.GenericTypeArguments[0]);

                return (IReference)TypeHelper.GetConstructorInfo(targetT, TypeHelper.TypeOf<Hash>.Type)
                    .Invoke(new object[] { hash });
            }
            else
            {
                return new Reference(hash);
            }
        }
        public override void WriteJson(JsonWriter writer, IReference value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class AABBJsonConverter : JsonConverter<AABB>
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override AABB ReadJson(JsonReader reader, Type objectType, AABB existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            JArray center = (JArray)jo["Center"];
            JArray extents = (JArray)jo["Extents"];

            return new AABB()
            {
                m_Center = new float3(
                    center[0].ToObject<float>(), 
                    center[1].ToObject<float>(), 
                    center[2].ToObject<float>()),
                m_Extents = new float3(
                    extents[0].ToObject<float>(),
                    extents[1].ToObject<float>(),
                    extents[2].ToObject<float>()),
            };
        }
        public override void WriteJson(JsonWriter writer, AABB value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Center");
            writer.WriteStartArray();
            writer.WriteValue(value.m_Center.x);
            writer.WriteValue(value.m_Center.y);
            writer.WriteValue(value.m_Center.z);
            writer.WriteEndArray();

            writer.WritePropertyName("Extents");
            writer.WriteStartArray();
            writer.WriteValue(value.m_Extents.x);
            writer.WriteValue(value.m_Extents.y);
            writer.WriteValue(value.m_Extents.z);
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
