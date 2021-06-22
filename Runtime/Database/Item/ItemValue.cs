using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(ItemValueJsonConverter))]
    public abstract class ItemValue : ICloneable
    {
        public string m_Name;

        public abstract object GetValue();
        public abstract object Clone();
    }
    [Serializable]
    public sealed class ItemValueNull : ItemValue
    {
        public override object GetValue() => null;
        public override object Clone()
        {
            return new ItemValueNull
            {
                m_Name = m_Name
            };
        }
    }
    [Serializable]
    public abstract class ItemValue<T> : ItemValue where T : IConvertible
    {
        public T m_Value;

        public override object GetValue() => m_Value;
    }

    #region Item Serializable Classes
    [Serializable]
    public sealed class SerializableItemIntValue : ItemValue<int>
    {
        public override object Clone()
        {
            return new SerializableItemIntValue
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    [Serializable]
    public sealed class SerializableItemFloatValue : ItemValue<float>
    {
        public override object Clone()
        {
            return new SerializableItemFloatValue
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    [Serializable]
    public sealed class SerializableItemStringValue : ItemValue<string>
    {
        public override object Clone()
        {
            return new SerializableItemStringValue
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    [Serializable]
    public sealed class SerializableItemBoolValue : ItemValue<bool>
    {
        public override object Clone()
        {
            return new SerializableItemBoolValue
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    #endregion

    #region Item Json Converter
    public class BaseSpecifiedConcreteClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(ItemValue).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null; // pretend TableSortRuleConvert is not specified (thus avoiding a stack overflow)
            return base.ResolveContractConverter(objectType);
        }
    }
    public class ItemValueJsonConverter : JsonConverter
    {
        static readonly JsonSerializerSettings SpecifiedSubclassConversion
            = new JsonSerializerSettings() { ContractResolver = new BaseSpecifiedConcreteClassConverter() };

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(ItemValue);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (!jo.TryGetValue("m_Value", out JToken value))
            {
                return JsonConvert.DeserializeObject<ItemValueNull>(jo.ToString(), SpecifiedSubclassConversion);
            }

            Type t = value.GetType();
            if (t.Equals(typeof(bool)))
            {
                return JsonConvert.DeserializeObject<SerializableItemBoolValue>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else if (t.Equals(typeof(float)))
            {
                return JsonConvert.DeserializeObject<SerializableItemFloatValue>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else if (t.Equals(typeof(int)))
            {
                return JsonConvert.DeserializeObject<SerializableItemIntValue>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else if (t.Equals(typeof(string)))
            {
                return JsonConvert.DeserializeObject<SerializableItemStringValue>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else
            {
                return JsonConvert.DeserializeObject<ItemValueNull>(jo.ToString(), SpecifiedSubclassConversion);
            }
        }
    }
    #endregion
}
