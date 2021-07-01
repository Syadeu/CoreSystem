﻿using Newtonsoft.Json;
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
            writer.WriteValue(refAsset.Data.m_Hash);

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
}
