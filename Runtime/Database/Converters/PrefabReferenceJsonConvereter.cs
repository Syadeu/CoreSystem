using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Internal;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Database.Converters
{
    [Preserve]
    internal sealed class PrefabReferenceJsonConvereter : JsonConverter<IPrefabReference>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override IPrefabReference ReadJson(JsonReader reader, Type objectType, IPrefabReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jToken = JToken.Load(reader);
            long value;

            // Prev
            if (jToken.Type == JTokenType.Object)
            {
                JObject jObj = (JObject)jToken;
                value = jObj.Value<long>("m_Idx");
            }
            else value = jToken.Value<long>();

            return (IPrefabReference)TypeHelper.GetConstructorInfo(objectType, TypeHelper.TypeOf<long>.Type)
                .Invoke(new object[] { value });
        }

        public override void WriteJson(JsonWriter writer, IPrefabReference value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Index);
        }
    }
}
