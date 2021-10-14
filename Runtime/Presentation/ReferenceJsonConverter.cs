using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Collections;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Converters
{
    [CustomJsonConverterAttribute, Preserve]
    internal sealed class ReferenceJsonConverter : JsonConverter<IFixedReference>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override IFixedReference ReadJson(JsonReader reader, Type objectType, IFixedReference existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jToken = JToken.Load(reader);

            Hash hash;
            // Prev
            if (jToken.Type == JTokenType.Object)
            {
                JObject jObj = (JObject)jToken;
                hash = jObj.Value<ulong>("Hash");
            }
            else hash = jToken.Value<ulong>();

            if (objectType.GenericTypeArguments.Length > 0)
            {
                Type targetT = typeof(Reference<>).MakeGenericType(objectType.GenericTypeArguments[0]);

                return (IFixedReference)TypeHelper.GetConstructorInfo(targetT, TypeHelper.TypeOf<Hash>.Type)
                    .Invoke(new object[] { hash });
            }
            else
            {
                return new Reference(hash);
            }
        }
        public override void WriteJson(JsonWriter writer, IFixedReference value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Hash);
        }
    }
}
