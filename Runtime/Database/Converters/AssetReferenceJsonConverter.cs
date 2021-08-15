using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Internal;
using System;

using UnityEngine.AddressableAssets;
using UnityEngine.Scripting;

namespace Syadeu.Database.Converters
{
    [Preserve]
    internal sealed class AssetReferenceJsonConverter : JsonConverter
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType.Equals(TypeHelper.TypeOf<AssetReference>.Type);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            AssetReference refAsset = value as AssetReference;
            writer.WriteValue(refAsset.AssetGUID);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jo = JToken.Load(reader);
            string guid = jo.Value<string>();
            return Activator.CreateInstance(objectType, guid);
        }
    }
}
