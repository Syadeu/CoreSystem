using MoonSharp.Interpreter;
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

        public abstract ValueType GetValueType();

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
            else if (t.Equals(typeof(float)) || t.Equals(typeof(double)))
            {
                return Double(name, Convert.ToDouble(value));
            }
            else if (t.Equals(typeof(string)))
            {
                return String(name, Convert.ToString(value));
            }
            else if (t.Equals(typeof(bool)))
            {
                return Bool(name, Convert.ToBoolean(value));
            }
            else if (value is Action action)
            {
                return Action(name, action);
            }
            else if (value is Closure func)
            {
                return Action(name, (Action)(() => func.Call()));
            }
            $"{value.GetType().Name} none setting".ToLog();
            throw new Exception();
        }
        public static ValuePair<int> Int(string name, int value)
            => new SerializableIntValuePair() { m_Name = name, m_Value = value };
        public static ValuePair<double> Double(string name, double value)
            => new SerializableDoubleValuePair() { m_Name = name, m_Value = value };
        public static ValuePair<string> String(string name, string value)
            => new SerializableStringValuePair() { m_Name = name, m_Value = value };
        public static ValuePair<bool> Bool(string name, bool value)
            => new SerializableBoolValuePair() { m_Name = name, m_Value = value };

        public static ValuePair<Action> Action(string name, Action func)
            => new SerializableActionValuePair() { m_Name = name, m_Value = func };
    }
    public abstract class ValuePair<T> : ValuePair, IEquatable<T>
    {
        public T m_Value;

        public override ValueType GetValueType()
        {
            if (typeof(T).Equals(typeof(int)))
            {
                return ValueType.Int32;
            }
            else if (typeof(T).Equals(typeof(float)) || typeof(T).Equals(typeof(double)))
            {
                return ValueType.Double;
            }
            else if (typeof(T).Equals(typeof(string)))
            {
                return ValueType.String;
            }
            else if (typeof(T).Equals(typeof(bool)))
            {
                return ValueType.Boolean;
            }
            else if (m_Value is Delegate)
            {
                return ValueType.Delegate;
            }
            return ValueType.Null;
        }

        public override object GetValue() => m_Value;
        public override bool Equals(ValuePair other)
            => (other is ValuePair<T> temp) && base.Equals(other) && Equals(temp.m_Value);
        public bool Equals(T other) => m_Value.Equals(other);
    }
    public abstract class ValueFuncPair<T> : ValuePair<T> where T : Delegate
    {
        public object Invoke(params object[] args)
        {
            return m_Value.DynamicInvoke(args);
        }
    }
    public sealed class ValueNull : ValuePair
    {
        public override ValueType GetValueType() => ValueType.Null;

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
        Double,
        String,
        Boolean,

        Delegate,
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
    public sealed class SerializableDoubleValuePair : ValuePair<double>
    {
        public override object Clone()
        {
            return new SerializableDoubleValuePair
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

    public sealed class SerializableActionValuePair : ValueFuncPair<Action>
    {
        public void Invoke() => Invoke(null);
        public override object Clone()
        {
            return new SerializableActionValuePair
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
                return JsonConvert.DeserializeObject<SerializableDoubleValuePair>(jo.ToString(), SpecifiedSubclassConversion);
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
