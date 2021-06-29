using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

#if UNITY_ADDRESSABLES
#endif

namespace Syadeu.Database
{
    internal sealed class ItemTypeJsonConverter : JsonConverter
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType.Equals(typeof(ItemTypeEntity));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject o;
            string name = value.GetType().AssemblyQualifiedName;

            if (value is ItemType)
            {
                o = JObject.Parse(
                    JsonConvert.SerializeObject(
                        value, typeof(ItemType), Formatting.Indented,
                        BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion)
                    );
                //BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion
                o.AddFirst(new JProperty("Type", name));
                o.WriteTo(writer);
            }
            else if (value is ItemUseableType)
            {
                o = JObject.Parse(
                    JsonConvert.SerializeObject(
                        value, typeof(ItemUseableType), Formatting.Indented,
                        BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion)
                    );

                o.AddFirst(new JProperty("Type", name));
                o.WriteTo(writer);
            }
            else
                throw new Exception();
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            Type t;
            if (jo.TryGetValue("Type", out var val))
            {
                t = Type.GetType(val.ToString());
                jo.Remove("Type");
            }
            else t = typeof(ItemType);

            ItemTypeEntity itemType = (ItemTypeEntity)JsonConvert.DeserializeObject(
                        jo.ToString(), t,
                        BaseSpecifiedConcreteClassConverter<ItemTypeEntity>.SpecifiedSubclassConversion
                        );

            return itemType;
        }
    }
}
