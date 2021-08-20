using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Converters
{
    [Preserve]
    internal sealed class ReferenceJsonConverter : JsonConverter<IReference>
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override IReference ReadJson(JsonReader reader, Type objectType, IReference existingValue, bool hasExistingValue, JsonSerializer serializer)
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
            writer.WriteValue(value.Hash);
        }
    }
}
