using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class HashJsonConverter : JsonConverter<Hash>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override Hash ReadJson(JsonReader reader, Type objectType, Hash existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jo = JToken.Load(reader);
            string temp = jo.ToString();

            if (ulong.TryParse(temp, out ulong hash)) return new Hash(hash);
            else return Hash.Empty;
        }

        public override void WriteJson(JsonWriter writer, Hash value, JsonSerializer serializer)
        {
            ulong hash = value;
            writer.WriteValue(hash);
        }
    }
}
