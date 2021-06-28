using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections;
using System.Linq;

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(ValuePairJsonConverter))]
    public abstract class ValuePair : ICloneable, IEquatable<ValuePair>
    {
        [UnityEngine.SerializeField][JsonProperty(Order = 0)] protected string m_Name;
        [JsonIgnore] protected uint m_Hash;

        [JsonIgnore] public string Name { get => m_Name; set => m_Name = value; }
        [JsonIgnore] public uint Hash
        {
            get
            {
                if (m_Hash == 0) m_Hash = FNV1a32.Calculate(m_Name);
                return m_Hash;
            }
        }

        public abstract ValueType GetValueType();

        public abstract object GetValue();
        public T GetValue<T>() => (T)GetValue();
        public abstract object Clone();
        public virtual bool Equals(ValuePair other) => Hash.Equals(other.Hash);

        public static ValuePair New(string name, object value)
        {
            if (value == null) return new ValueNull(name);

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
            else if (value is IList list)
            {
                return Array(name, list);
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
            => new SerializableIntValuePair() { m_Name = name, m_Value = value, m_Hash = FNV1a32.Calculate(name) };
        public static ValuePair<double> Double(string name, double value)
            => new SerializableDoubleValuePair() { m_Name = name, m_Value = value, m_Hash = FNV1a32.Calculate(name) };
        public static ValuePair<string> String(string name, string value)
            => new SerializableStringValuePair() { m_Name = name, m_Value = value, m_Hash = FNV1a32.Calculate(name) };
        public static ValuePair<bool> Bool(string name, bool value)
            => new SerializableBoolValuePair() { m_Name = name, m_Value = value, m_Hash = FNV1a32.Calculate(name) };

        public static ValuePair<IList> Array(string name, params int[] values)
            => new SerializableArrayValuePair() { m_Name = name, m_Value = values, m_Hash = FNV1a32.Calculate(name) };
        public static ValuePair<IList> Array(string name, IList values)
            => new SerializableArrayValuePair() { m_Name = name, m_Value = values, m_Hash = FNV1a32.Calculate(name) };

        public static ValuePair<Action> Action(string name, Action func)
            => new SerializableActionValuePair() { m_Name = name, m_Value = func, m_Hash = FNV1a32.Calculate(name) };
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
            else if (m_Value is IList)
            {
                return ValueType.Array;
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
        public ValueNull(string name)
        {
            m_Name = name;
            m_Hash = FNV1a32.Calculate(name);
        }

        public override ValueType GetValueType() => ValueType.Null;

        public override object GetValue() => null;
        public override object Clone()
        {
            return new ValueNull(m_Name);
        }
    }
    public enum ValueType
    {
        Null,

        Int32,
        Double,
        String,
        Boolean,

        Array,

        Delegate,
    }

    [Serializable]
    public sealed class ValuePairContainer : IList, ICloneable
    {
        [UnityEngine.SerializeReference][JsonProperty] private ValuePair[] m_Values;
        [MoonSharpHidden] public ValuePair this[int i]
        {
            get => m_Values[i];
            set => m_Values[i] = ValuePair.New(m_Values[i].Name, value);
        }
        [MoonSharpHidden] object IList.this[int index]
        {
            get => m_Values[index];
            set => m_Values[index] = ValuePair.New(m_Values[index].Name, value);
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
                if (m_Values[i].Name.Equals(name)) return i;
            }
            return -1;
        }
        private int GetValuePairIdx(uint hash)
        {
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i].Hash.Equals(hash)) return i;
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
        public bool Contains(uint hash) => GetValuePairIdx(hash) >= 0;

        public ValuePair GetValuePair(string name) => m_Values[GetValuePairIdx(name)];
        public ValuePair GetValuePair(uint hash) => m_Values[GetValuePairIdx(hash)];
        public object GetValue(string name) => GetValuePair(name).GetValue();
        public object GetValue(uint hash) => GetValuePair(hash).GetValue();
        public T GetValue<T>(string name) => (T)GetValue(name);
        public T GetValue<T>(uint hash) => (T)GetValue(hash);
        public void SetValue(string name, object value) => m_Values[GetValuePairIdx(name)] = ValuePair.New(name, value);
        public void SetValue(uint hash, object value)
        {
            int idx = GetValuePairIdx(hash);
            var temp = m_Values[idx];
            m_Values[idx] = ValuePair.New(temp.Name, value);
        }
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
        public void Add(ValuePair valuePair)
        {
            if (Contains(valuePair.Hash)) throw new Exception();

            var temp = m_Values.ToList();
            temp.Add(valuePair);
            m_Values = temp.ToArray();
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
        public void AddRange(params ValuePair[] values)
        {
            var temp = m_Values.ToList();
            temp.AddRange(values);
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
    [Serializable] public sealed class SerializableIntValuePair : ValuePair<int>
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
    [Serializable] public sealed class SerializableDoubleValuePair : ValuePair<double>
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
    [Serializable] public sealed class SerializableStringValuePair : ValuePair<string>
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
    [Serializable] public sealed class SerializableBoolValuePair : ValuePair<bool>
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

    public sealed class SerializableArrayValuePair : ValuePair<IList>
    {
        public override object Clone()
        {
            return new SerializableArrayValuePair
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


}
