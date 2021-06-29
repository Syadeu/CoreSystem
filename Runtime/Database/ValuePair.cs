using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections;

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
