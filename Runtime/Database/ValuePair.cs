﻿using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Syadeu.Mono;
using System;
using System.Collections;

using Syadeu.Database.Lua;

namespace Syadeu.Database
{
    [Serializable] [JsonConverter(typeof(Converters.ValuePairJsonConverter))]
    public abstract class ValuePair : ICloneable, IEquatable<ValuePair>
    {
        [UnityEngine.SerializeField][JsonProperty(Order = 0)] protected string m_Name;
        [JsonIgnore] protected Hash m_Hash;

        [JsonIgnore] public string Name { get => m_Name; set => m_Name = value; }
        [JsonIgnore] public Hash Hash
        {
            get
            {
                if (m_Hash == Hash.Empty) m_Hash = Hash.NewHash(m_Name);
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
            else if (value is Action action) return Action(name, action);
            else if (value is Action<CreatureBrainProxy> actiont) return Action(name, actiont);
            else if (value is Action<CreatureBrain> actionta) return Action(name, actionta);
            else if (value is Closure func)
            {
                return Closure(name, func);
            }
            $"{value.GetType().Name} none setting".ToLog();
            throw new Exception();
        }
        public static ValuePair<int> Int(string name, int value)
            => new SerializableIntValuePair() { m_Name = name, m_Value = value, m_Hash = Hash.NewHash(name) };
        public static ValuePair<double> Double(string name, double value)
            => new SerializableDoubleValuePair() { m_Name = name, m_Value = value, m_Hash = Hash.NewHash(name) };
        public static ValuePair<string> String(string name, string value)
            => new SerializableStringValuePair() { m_Name = name, m_Value = value, m_Hash = Hash.NewHash(name) };
        public static ValuePair<bool> Bool(string name, bool value)
            => new SerializableBoolValuePair() { m_Name = name, m_Value = value, m_Hash = Hash.NewHash(name) };

        public static ValuePair<ValuePairContainer> Object(string name, params ValuePair[] values)
            => new SerializableObjectValuePair() { m_Name = name, m_Value = new ValuePairContainer(values), m_Hash = Hash.NewHash(name) };
        public static ValuePair<IList> Array(string name, IList values)
            => new SerializableArrayValuePair() { m_Name = name, m_Value = values, m_Hash = Hash.NewHash(name) };

        public static ValueFuncPair<T> Action<T>(string name, T func) where T : Delegate
            => new ValueFuncPair<T>() { m_Name = name, m_Value = func, m_Hash = Hash.NewHash(name) };
        public static SerializableClosureValuePair Closure(string name, Closure func)
            => new SerializableClosureValuePair() { m_Name = name, m_Value = func, m_Hash = Hash.NewHash(name) };
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
            else if (m_Value is Delegate || m_Value is Closure)
            {
                return ValueType.Delegate;
            }
            return ValueType.Null;
        }

        public override object GetValue() => m_Value;
        public override bool Equals(ValuePair other)
            => (other is ValuePair<T> temp) && base.Equals(other) && m_Value.Equals(temp.m_Value);
        public bool Equals(T other) => m_Value.Equals(other);
    }
    public abstract class ValueFuncPair : ValuePair
    {
        public override ValueType GetValueType() => ValueType.Delegate;
        
        public abstract object Invoke(params object[] args);
    }
    [Serializable]
    public sealed class ValueFuncPair<T> : ValueFuncPair where T : Delegate
    {
        [JsonIgnore] public T m_Value;

        public override object GetValue() => m_Value;
        public override bool Equals(ValuePair other) 
            => base.Equals(other) && (other is ValueFuncPair<T> temp) && m_Value.Equals(temp.m_Value);

        public override object Invoke(params object[] args)
        {
            return m_Value.DynamicInvoke(args);
        }
        public override object Clone()
        {
            return new ValueFuncPair<T>
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }
    public sealed class ValueNull : ValuePair
    {
        public ValueNull(string name)
        {
            m_Name = name;
            m_Hash = Hash.NewHash(name);
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
    [Serializable] public sealed class SerializableObjectValuePair : ValuePair<ValuePairContainer>
    {
        public override object Clone()
        {
            return new SerializableObjectValuePair
            {
                m_Name = m_Name,
                m_Value = (ValuePairContainer)m_Value.Clone()
            };
        }
    }

    public sealed class SerializableClosureValuePair : ValueFuncPair
    {
        [JsonIgnore] public Closure m_Value;

        public override object GetValue() => m_Value;
        public override bool Equals(ValuePair other)
            => base.Equals(other) && (other is SerializableClosureValuePair temp) && m_Value.Equals(temp.m_Value);

        public override object Invoke(params object[] args) => m_Value.Call(args);
        public override object Clone()
        {
            return new SerializableClosureValuePair
            {
                m_Name = m_Name,
                m_Value = m_Value
            };
        }
    }

#endregion
}
