using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace Syadeu.Database
{
#if UNITY_ADDRESSABLES
    internal sealed class AssetReferenceJsonConverter : JsonConverter
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType.Equals(typeof(string));

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            AssetReference refAsset = value as AssetReference;
            writer.WriteValue(refAsset.AssetGUID);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jo = JToken.Load(reader);
            string guid = jo.Value<string>();
            return new AssetReference(guid);
        }
    }

#endif
}
