using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(ValuePairJsonConverter))]
    public abstract class ValuePair : ICloneable, IEquatable<ValuePair>
    {
        [JsonProperty(Order = 0)] public string m_Name;

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
        [JsonProperty(Order = 1)] public T m_Value;

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

    [Serializable]
    public sealed class ValuePairContainer : IList, ICloneable
    {
        [MoonSharpVisible(true)][UnityEngine.SerializeReference][JsonProperty] private ValuePair[] m_Values;
        [MoonSharpHidden] public ValuePair this[int i]
        {
            get => m_Values[i];
            set => m_Values[i] = ValuePair.New(m_Values[i].m_Name, value);
        }
        [MoonSharpHidden] object IList.this[int index]
        {
            get => m_Values[index];
            set => m_Values[index] = ValuePair.New(m_Values[index].m_Name, value);
        }
        [JsonIgnore] public int Count => m_Values.Length;

        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public bool IsSynchronized => throw new NotImplementedException();
        public object SyncRoot => throw new NotImplementedException();

        [MoonSharpHidden] public ValuePairContainer(params ValuePair[] values)
        {
            m_Values = values == null ? new ValuePair[0] : values;
        }

        private int GetValuePairIdx(string name)
        {
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i].m_Name.Equals(name)) return i;
            }
            return -1;
        }
        public int IndexOf(object value)
        {
            if (value is ValuePair pair)
            {
                for (int i = 0; i < m_Values.Length; i++)
                {
                    if (m_Values[i].Equals(pair)) return i;
                }
            }
            else if (value is string name)
            {
                return GetValuePairIdx(name);
            }

            throw new Exception();
        }

        public bool Contains(object value)
        {
            if (value is ValuePair pair)
            {
                for (int i = 0; i < m_Values.Length; i++)
                {
                    if (m_Values[i].Equals(pair)) return true;
                }
            }
            else if (value is string name)
            {
                return Contains(name);
            }

            return false;
        }
        public bool Contains(string name) => GetValuePairIdx(name) >= 0;

        public object GetValue(string name) => m_Values[GetValuePairIdx(name)].GetValue();
        public void SetValue(string name, object value) => m_Values[GetValuePairIdx(name)] = ValuePair.New(name, value);
        public int Add(object value)
        {
            var temp = m_Values.ToList();
            if (value is JObject jobj)
            {
                temp.Add(jobj.ToObject<ValuePair>());
            }
            else temp.Add(ValuePair.New("New Value", value));
            m_Values = temp.ToArray();
            return m_Values.Length - 1;
        }
        public void Add(string name, object value)
        {
            if (Contains(name)) throw new Exception();

            var temp = m_Values.ToList();
            temp.Add(ValuePair.New(name, value));
            m_Values = temp.ToArray();
        }
        public void Add<T>(string name, T value)
        {
            var temp = m_Values.ToList();
            temp.Add(ValuePair.New(name, value));
            m_Values = temp.ToArray();
        }

        public void Clear() => m_Values = new ValuePair[0];
        public void Remove(object item)
        {
            if (item is string name)
            {
                int i = GetValuePairIdx(name);
                if (i < 0) return;
                RemoveAt(i);
            }
            else if (item is ValuePair pair)
            {
                for (int i = 0; i < m_Values.Length; i++)
                {
                    if (m_Values.Equals(pair))
                    {
                        RemoveAt(i);
                        return;
                    }
                }
            }
        }
        public void RemoveAt(int i)
        {
            var temp = m_Values.ToList();
            temp.RemoveAt(i);
            m_Values = temp.ToArray();
        }

        public object Clone() => new ValuePairContainer(m_Values.ToArray());

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator() => m_Values.GetEnumerator();
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
