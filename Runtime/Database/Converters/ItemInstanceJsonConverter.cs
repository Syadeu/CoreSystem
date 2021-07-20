using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;

namespace Syadeu.Database.Converters
{
    internal sealed class ItemInstanceJsonConverter : JsonConverter
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType.Equals(TypeHelper.TypeOf<ItemInstance>.Type);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ItemInstance refAsset = value as ItemInstance;

            writer.WriteStartObject();

            writer.WritePropertyName("Data");
            writer.WriteValue(refAsset.Data.Hash);

            writer.WritePropertyName("Guid");
            writer.WriteValue(refAsset.Hash);

            writer.WriteEndObject();
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            ulong dataHash = jo["Data"].ToObject<ulong>();
            ulong hash = jo["Hash"].ToObject<ulong>();

            return new ItemInstance(dataHash, hash);
        }
    }
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
}
