using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    internal sealed class ItemInstanceJsonConverter : JsonConverter
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType.Equals(typeof(ItemInstance));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ItemInstance refAsset = value as ItemInstance;

            writer.WriteStartObject();

            writer.WritePropertyName("Data");
            writer.WriteValue(refAsset.Data.m_Guid);

            writer.WritePropertyName("Guid");
            writer.WriteValue(refAsset.Guid);

            writer.WriteEndObject();
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string dataGuid = jo["Data"].ToString();
            string guid = jo["Guid"].ToString();

            return new ItemInstance(dataGuid, guid);
        }
    }

    internal sealed class ItemTypeJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanRead => false;

        public override bool CanConvert(Type objectType) => objectType.Equals(typeof(ItemTypeEntity));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject o = (JObject)JToken.FromObject(value);
            if (value is ItemType)
            {
                //BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion
                o.AddFirst(new JProperty("Type", ClassType.Common));
                o.WriteTo(writer);
            }
            else if (value is ItemUseableType)
            {
                o.AddFirst(new JProperty("Type", ClassType.Useable));
                o.WriteTo(writer);
            }
            else
                throw new Exception();
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            ItemTypeEntity itemType;


            if (!jo.TryGetValue("m_Values", out JToken _))
            {
                ItemUseableType temp = new ItemUseableType(
                    jo["m_Name"].ToString(), jo["m_Guid"].ToString())
                {
                    m_RemoveOnUse = jo["m_RemoveOnUse"].ToObject<bool>(),
                    m_OnUse = jo["m_OnUse"].ToObject<ValuePairContainer>()
                };

                itemType = temp;
            }
            else
            {
                ItemType temp = new ItemType(
                    jo["m_Name"].ToString(), jo["m_Guid"].ToString())
                {
                    m_Values = jo["m_Values"].ToObject<ValuePairContainer>()
                };

                itemType = temp;
            }

            return itemType;
        }

        private enum ClassType
        {
            Common,
            Useable
        }
    }
}
