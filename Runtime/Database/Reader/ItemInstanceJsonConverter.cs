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
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType.Equals(typeof(ItemTypeEntity));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject o;
            if (value is ItemType)
            {
                o = JObject.Parse(
                    JsonConvert.SerializeObject(
                        value, typeof(ItemType), Formatting.Indented,
                        BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion)
                    );
                //BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion
                o.AddFirst(new JProperty("Type", ClassType.Common));
                o.WriteTo(writer);
            }
            else if (value is ItemUseableType)
            {
                o = JObject.Parse(
                    JsonConvert.SerializeObject(
                        value, typeof(ItemUseableType), Formatting.Indented,
                        BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion)
                    );

                o.AddFirst(new JProperty("Type", ClassType.Useable));
                o.WriteTo(writer);
            }
            else
                throw new Exception();
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            ClassType classType = (ClassType)jo["Type"].ToObject<int>();
            jo.Remove("Type");

            ItemTypeEntity itemType;
            switch (classType)
            {
                case ClassType.Useable:
                    itemType = (ItemUseableType)JsonConvert.DeserializeObject(
                        jo.ToString(), typeof(ItemUseableType),
                        BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion
                        );
                    break;
                default:
                    itemType = (ItemType)JsonConvert.DeserializeObject(
                        jo.ToString(), typeof(ItemType),
                        BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion
                        );
                    break;
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
