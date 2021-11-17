// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using MoonSharp.Interpreter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections;
using System.Linq;

namespace Syadeu.Collections
{
    [Serializable]
    public sealed class ValuePairContainer : ICloneable
    {
        [UnityEngine.SerializeReference][JsonProperty(Order = 0, PropertyName = "Values")] private ValuePair[] m_Values;
        [MoonSharpHidden] public ValuePair this[int i]
        {
            get => m_Values[i];
            set => m_Values[i] = ValuePair.New(m_Values[i].Name, value);
        }
        //[MoonSharpHidden] object IList.this[int index]
        //{
        //    get => m_Values[index];
        //    set => m_Values[index] = ValuePair.New(m_Values[index].Name, value);
        //}
        [JsonIgnore] public int Count => m_Values.Length;

        [JsonIgnore] public bool IsFixedSize => false;
        [JsonIgnore] public bool IsReadOnly => false;
        [JsonIgnore] public bool IsSynchronized => throw new NotImplementedException();
        [JsonIgnore] public object SyncRoot => throw new NotImplementedException();

        [MoonSharpHidden] public ValuePairContainer(params ValuePair[] values)
        {
            m_Values = values == null ? new ValuePair[0] : values;
        }
        [JsonConstructor] public ValuePairContainer()
        {
            m_Values = Array.Empty<ValuePair>();
        }

        private int GetValuePairIdx(string name)
        {
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (m_Values[i].Name.Equals(name)) return i;
            }
            return -1;
        }
        private int GetValuePairIdx(Hash hash)
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
        public bool Contains(Hash hash) => GetValuePairIdx(hash) >= 0;

        public ValuePair GetValuePair(string name)
        {
            if (!Contains(name))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"{nameof(ValuePairContainer)} does not have name of {name} value.");
                return null;
            }
            return m_Values[GetValuePairIdx(name)];
        }
        public ValuePair GetValuePair(Hash hash)
        {
            if (!Contains(hash))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"{nameof(ValuePairContainer)} does not have hash of {hash} value.");
                return null;
            }
            return m_Values[GetValuePairIdx(hash)];
        }
        public object GetValue(string name)
        {
            ValuePair valuePair = GetValuePair(name);
            if (valuePair == null)
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"{nameof(ValuePairContainer)} does not have name of {name} value.");
                return null;
            }

            return valuePair.GetValue();
        }
        public object GetValue(Hash hash)
        {
            ValuePair valuePair = GetValuePair(hash);
            if (valuePair == null)
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"{nameof(ValuePairContainer)} does not have hash of {hash} value.");
                return null;
            }

            return valuePair.GetValue();
        }
        public T GetValue<T>(string name)
        {
            object value = GetValue(name);

            if (value == null) return default(T);
            return (T)value;
        }
        public T GetValue<T>(Hash hash)
        {
            object value = GetValue(hash);

            if (value == null) return default(T);
            return (T)value;
        }
        public ValuePair[] GetValuePairs(ValueType valueType) => m_Values.Where((other) => other.GetValueType() == valueType).ToArray();
        public ValuePair[] GetValuePairs(Func<ValuePair, bool> predictate) => m_Values.Where((other) => predictate.Invoke(other)).ToArray();
        public void SetValue(string name, object value)
        {
            if (!Contains(name))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"{nameof(ValuePairContainer)} does not have name of {name} value.");
                return;
            }
            m_Values[GetValuePairIdx(name)] = ValuePair.New(name, value);
        }
        public void SetValue(Hash hash, object value)
        {
            if (!Contains(hash))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"{nameof(ValuePairContainer)} does not have hash of {hash} value.");
                return;
            }

            int idx = GetValuePairIdx(hash);
            var temp = m_Values[idx];
            m_Values[idx] = ValuePair.New(temp.Name, value);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Index</returns>
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
            if (Contains(valuePair.Hash))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    "Atempt to add same ValuePair.");
                return;
            }

            var temp = m_Values.ToList();
            temp.Add(valuePair);
            m_Values = temp.ToArray();
        }
        public void Add(string name, object value)
        {
            if (Contains(name))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"This {nameof(ValuePairContainer)} already contains name of {name}.");
                return;
            }

            var temp = m_Values.ToList();
            temp.Add(ValuePair.New(name, value));
            m_Values = temp.ToArray();
        }
        public void Add<T>(string name, T value)
        {
            if (Contains(name))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"This {nameof(ValuePairContainer)} already contains name of {name}.");
                return;
            }

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
        public void AddRange(ValuePairContainer container) => AddRange(container.m_Values);

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
            if (i >= Count)
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    $"{nameof(ValuePairContainer)} raised Out of Range. Request remove index {i} exceed container count {Count}.");
                return;
            }

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
    //[Serializable]
    //public sealed class ValuePairContainer<T> : IList, ICloneable where T : IConvertible
    //{
    //    [UnityEngine.SerializeReference][JsonProperty] private ValuePair<T>[] m_Values;
    //    [MoonSharpHidden] public ValuePair<T> this[int i]
    //    {
    //        get => m_Values[i];
    //        set => m_Values[i] = (ValuePair<T>)ValuePair.New(m_Values[i].Name, value);
    //    }
    //    [MoonSharpHidden] object IList.this[int index]
    //    {
    //        get => m_Values[index];
    //        set => m_Values[index] = (ValuePair<T>)ValuePair.New(m_Values[index].Name, value);
    //    }
    //    [JsonIgnore] public int Count => m_Values.Length;

    //    public bool IsFixedSize => false;
    //    public bool IsReadOnly => false;
    //    public bool IsSynchronized => throw new NotImplementedException();
    //    public object SyncRoot => throw new NotImplementedException();

    //    [MoonSharpHidden] public ValuePairContainer(params ValuePair<T>[] values)
    //    {
    //        m_Values = values == null ? new ValuePair<T>[0] : values;
    //    }

    //    private int GetValuePairIdx(string name)
    //    {
    //        for (int i = 0; i < m_Values.Length; i++)
    //        {
    //            if (m_Values[i].Name.Equals(name)) return i;
    //        }
    //        return -1;
    //    }
    //    private int GetValuePairIdx(uint hash)
    //    {
    //        for (int i = 0; i < m_Values.Length; i++)
    //        {
    //            if (m_Values[i].Hash.Equals(hash)) return i;
    //        }
    //        return -1;
    //    }
    //    public int IndexOf(object value)
    //    {
    //        if (value is ValuePair pair)
    //        {
    //            for (int i = 0; i < m_Values.Length; i++)
    //            {
    //                if (m_Values[i].Equals(pair)) return i;
    //            }
    //        }
    //        else if (value is string name)
    //        {
    //            return GetValuePairIdx(name);
    //        }

    //        throw new Exception();
    //    }

    //    public bool Contains(object value)
    //    {
    //        if (value is ValuePair pair)
    //        {
    //            for (int i = 0; i < m_Values.Length; i++)
    //            {
    //                if (m_Values[i].Equals(pair)) return true;
    //            }
    //        }
    //        else if (value is string name)
    //        {
    //            return Contains(name);
    //        }

    //        return false;
    //    }
    //    public bool Contains(string name) => GetValuePairIdx(name) >= 0;
    //    public bool Contains(uint hash) => GetValuePairIdx(hash) >= 0;

    //    public ValuePair<T> GetValuePair(string name) => m_Values[GetValuePairIdx(name)];
    //    public ValuePair<T> GetValuePair(uint hash) => m_Values[GetValuePairIdx(hash)];
    //    public T GetValue(string name) => (T)GetValuePair(name).GetValue();
    //    public T GetValue(uint hash) => (T)GetValuePair(hash).GetValue();
    //    public ValuePair<T>[] GetValuePairs(ValueType valueType) => m_Values.Where((other) => other.GetValueType() == valueType).ToArray();
    //    public ValuePair<T>[] GetValuePairs(Func<ValuePair, bool> predictate) => m_Values.Where((other) => predictate.Invoke(other)).ToArray();
    //    public void SetValue(string name, T value) => m_Values[GetValuePairIdx(name)] = (ValuePair<T>)ValuePair.New(name, value);
    //    public void SetValue(uint hash, T value)
    //    {
    //        int idx = GetValuePairIdx(hash);
    //        var temp = m_Values[idx];
    //        m_Values[idx] = (ValuePair<T>)ValuePair.New(temp.Name, value);
    //    }
    //    public int Add(object value)
    //    {
    //        var temp = m_Values.ToList();
    //        if (value is JObject jobj)
    //        {
    //            temp.Add(jobj.ToObject<ValuePair<T>>());
    //        }
    //        else temp.Add((ValuePair<T>)ValuePair.New("New Value", value));
    //        m_Values = temp.ToArray();
    //        return m_Values.Length - 1;
    //    }
    //    public void Add(ValuePair<T> valuePair)
    //    {
    //        if (Contains(valuePair.Hash)) throw new Exception();

    //        var temp = m_Values.ToList();
    //        temp.Add(valuePair);
    //        m_Values = temp.ToArray();
    //    }
    //    public void Add(string name, T value)
    //    {
    //        if (Contains(name)) throw new Exception();

    //        var temp = m_Values.ToList();
    //        temp.Add((ValuePair<T>)ValuePair.New(name, value));
    //        m_Values = temp.ToArray();
    //    }
    //    public void AddRange(params ValuePair<T>[] values)
    //    {
    //        var temp = m_Values.ToList();
    //        temp.AddRange(values);
    //        m_Values = temp.ToArray();
    //    }
    //    public void AddRange(ValuePairContainer<T> container) => AddRange(container.m_Values);

    //    public void Clear() => m_Values = new ValuePair<T>[0];
    //    public void Remove(object item)
    //    {
    //        if (item is string name)
    //        {
    //            int i = GetValuePairIdx(name);
    //            if (i < 0) return;
    //            RemoveAt(i);
    //        }
    //        else if (item is ValuePair pair)
    //        {
    //            for (int i = 0; i < m_Values.Length; i++)
    //            {
    //                if (m_Values.Equals(pair))
    //                {
    //                    RemoveAt(i);
    //                    return;
    //                }
    //            }
    //        }
    //    }
    //    public void RemoveAt(int i)
    //    {
    //        var temp = m_Values.ToList();
    //        temp.RemoveAt(i);
    //        m_Values = temp.ToArray();
    //    }

    //    public object Clone() => new ValuePairContainer<T>(m_Values.ToArray());

    //    public void Insert(int index, object value)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void CopyTo(Array array, int index)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public IEnumerator GetEnumerator() => m_Values.GetEnumerator();
    //}
}
