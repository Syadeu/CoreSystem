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

using Newtonsoft.Json;
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Presentation.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Syadeu.Presentation
{
    [Serializable]
    [JsonArray]
    //[JsonConverter(typeof(AttributeArrayJsonConverter))]
    public sealed class AttributeArray : ICloneable, IList<Reference<AttributeBase>>
    {
        [UnityEngine.SerializeField]
        [JsonProperty(Order = -900, PropertyName = "Attributes")]
        public Reference<AttributeBase>[] m_Attributes = Array.Empty<Reference<AttributeBase>>();

        public Reference<AttributeBase> this[int index]
        {
            get => m_Attributes[index];
            set => m_Attributes[index] = value;
        }

        public int Length => m_Attributes.Length;
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;

        public int Count => Length;
        public bool IsSynchronized => true;
        public object SyncRoot => throw new NotImplementedException();

        Reference<AttributeBase> IList<Reference<AttributeBase>>.this[int index]
        {
            get => m_Attributes[index];
            set => m_Attributes[index] = value;
        }

        public AttributeArray() { }
        [JsonConstructor]
        public AttributeArray(IEnumerable<Reference<AttributeBase>> attributes)
        {
            m_Attributes = attributes.ToArray();
        }

        public object Clone()
        {
            AttributeArray obj = (AttributeArray)MemberwiseClone();

            obj.m_Attributes = (Reference<AttributeBase>[])m_Attributes.Clone();

            return obj;
        }

        public int IndexOf(Reference<AttributeBase> item) => m_Attributes.IndexOf(item);

        public void Insert(int index, Reference<AttributeBase> item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            if (m_Attributes.RemoveAtSwapBack(index))
            {
                Array.Resize(ref m_Attributes, m_Attributes.Length - 1);
            }
        }
        public void Add(Reference<AttributeBase> item)
        {
            Array.Resize(ref m_Attributes, m_Attributes.Length + 1);
            m_Attributes[m_Attributes.Length - 1] = item;
        }

        public void Clear()
        {
            m_Attributes = Array.Empty<Reference<AttributeBase>>();
        }

        public bool Contains(Reference<AttributeBase> item) => m_Attributes.Contains(item);
        public void CopyTo(Reference<AttributeBase>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Reference<AttributeBase> item)
        {
            if (m_Attributes.RemoveForSwapBack(item))
            {
                Array.Resize(ref m_Attributes, m_Attributes.Length - 1);
                return true;
            }
            return false;
        }

        public IEnumerator<Reference<AttributeBase>> GetEnumerator() => ((IList<Reference<AttributeBase>>)m_Attributes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => m_Attributes.GetEnumerator();

        public static implicit operator Reference<AttributeBase>[](AttributeArray t) => t.m_Attributes;
        public static implicit operator AttributeArray(Reference<AttributeBase>[] t)=> new AttributeArray(t);
    }

    //internal sealed class AttributeArrayJsonConverter : JsonConverter<AttributeArray>
    //{
    //    public override bool CanRead => true;
    //    public override bool CanWrite => true;

    //    public override AttributeArray ReadJson(JsonReader reader, Type objectType, AttributeArray existingValue, bool hasExistingValue, JsonSerializer serializer)
    //    {

    //    }
    //    public override void WriteJson(JsonWriter writer, AttributeArray value, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
