using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using UnityEngine.Scripting;

namespace Syadeu.Database.Converters
{
    [Preserve]
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
