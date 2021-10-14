using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Reflection;
using UnityEngine.Scripting;

namespace Syadeu.Collections.Converters
{
    [Preserve]
    internal sealed class PrefabReferenceJsonConvereter : JsonConverter<IPrefabReference>
    {
        //private ConstructorInfo m_Constructor;
        private Type[] m_ConstructorParam;

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public PrefabReferenceJsonConvereter() : base()
        {
            m_ConstructorParam = new Type[] { typeof(long) };
        }

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

            return (IPrefabReference)objectType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, CallingConventions.HasThis, m_ConstructorParam, null).Invoke(new object[] { value });
        }

        public override void WriteJson(JsonWriter writer, IPrefabReference value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Index);
        }
    }
}
