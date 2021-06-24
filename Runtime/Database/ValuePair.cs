using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(ValuePairJsonConverter))]
    public abstract class ValuePair : ICloneable, IEquatable<ValuePair>
    {
        public string m_Name;

        public abstract ValueType Type { get; }

        public abstract object GetValue();
        public abstract object Clone();
        public virtual bool Equals(ValuePair other) => m_Name.Equals(other.m_Name);

        public static ValuePair New(string name, object value)
        {
            if (value == null) return new ValueNull { m_Name = name };

            Type t = value.GetType();
            if (t.Equals(typeof(int)))
            {
                return Int(name, Convert.ToInt32(value));
            }
            else if (t.Equals(typeof(float)))
            {
                return Float(name, Convert.ToSingle(value));
            }
            else if (t.Equals(typeof(string)))
            {
                return String(name, Convert.ToString(value));
            }
            else if (t.Equals(typeof(bool)))
            {
                return Bool(name, Convert.ToBoolean(value));
            }
            throw new Exception();
        }
        public static ValuePair<int> Int(string name, int value)
            => new SerializableIntValuePair() { m_Name = name, m_Value = value };
        public static ValuePair<float> Float(string name, float value)
            => new SerializableFloatValuePair() { m_Name = name, m_Value = value };
        public static ValuePair<string> String(string name, string value)
            => new SerializableStringValuePair() { m_Name = name, m_Value = value };
        public static ValuePair<bool> Bool(string name, bool value)
            => new SerializableBoolValuePair() { m_Name = name, m_Value = value };
    }
    public abstract class ValuePair<T> : ValuePair, IEquatable<T> where T : IConvertible
    {
        public T m_Value;

        public override ValueType Type
        {
            get
            {
                if (typeof(T).Equals(typeof(int)))
                {
                    return ValueType.Int32;
                }
                else if (typeof(T).Equals(typeof(float)))
                {
                    return ValueType.Single;
                }
                else if (typeof(T).Equals(typeof(string)))
                {
                    return ValueType.String;
                }
                else if (typeof(T).Equals(typeof(bool)))
                {
                    return ValueType.Boolean;
                }
                return ValueType.Null;
            }
        }

        public override object GetValue() => m_Value;
        public override bool Equals(ValuePair other)
            => (other is ValuePair<T> temp) && base.Equals(other) && Equals(temp.m_Value);
        public bool Equals(T other) => m_Value.Equals(other);
    }
    public sealed class ValueNull : ValuePair
    {
        public override ValueType Type => ValueType.Null;

        public override object GetValue() => null;
        public override object Clone()
        {
            return new ValueNull
            {
                m_Name = m_Name
            };
        }
    }
    public enum ValueType
    {
        Null,

        Int32,
        Single,
        String,
        Boolean
    }

    #region Serializable Classes
    public sealed class SerializableIntValuePair : ValuePair<int>
    {
        public override object Clone()
        {
            return new SerializableIntValuePair
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    public sealed class SerializableFloatValuePair : ValuePair<float>
    {
        public override object Clone()
        {
            return new SerializableFloatValuePair
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    public sealed class SerializableStringValuePair : ValuePair<string>
    {
        public override object Clone()
        {
            return new SerializableStringValuePair
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    public sealed class SerializableBoolValuePair : ValuePair<bool>
    {
        public override object Clone()
        {
            return new SerializableBoolValuePair
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    #endregion

    #region Json Converter
    public class BaseSpecifiedConcreteClassConverter : DefaultContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(ValuePair).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null; // pretend TableSortRuleConvert is not specified (thus avoiding a stack overflow)
            return base.ResolveContractConverter(objectType);
        }
    }
    public class ValuePairJsonConverter : JsonConverter
    {
        static readonly JsonSerializerSettings SpecifiedSubclassConversion
            = new JsonSerializerSettings() { ContractResolver = new BaseSpecifiedConcreteClassConverter() };

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(ValuePair);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (!jo.TryGetValue("m_Value", out JToken value))
            {
                return JsonConvert.DeserializeObject<ValueNull>(jo.ToString(), SpecifiedSubclassConversion);
            }

            if (value.Type == JTokenType.Boolean)
            {
                return JsonConvert.DeserializeObject<SerializableBoolValuePair>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else if (value.Type == JTokenType.Float)
            {
                return JsonConvert.DeserializeObject<SerializableFloatValuePair>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else if (value.Type == JTokenType.Integer)
            {
                return JsonConvert.DeserializeObject<SerializableIntValuePair>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else if (value.Type == JTokenType.String)
            {
                return JsonConvert.DeserializeObject<SerializableStringValuePair>(jo.ToString(), SpecifiedSubclassConversion);
            }
            else
            {
                return JsonConvert.DeserializeObject<ValueNull>(jo.ToString(), SpecifiedSubclassConversion);
            }
        }
    }
    #endregion
}
