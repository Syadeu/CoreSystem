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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections.Buffer.LowLevel;
using System;

namespace Syadeu.Collections.Buffer
{
    /// <summary>
    /// 재배열을 최대한 피하고, 고정된 배열로 제네릭 리스트와 같이 사용할 수 있도록 합니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class FixedList<T>
        where T : IEquatable<T>
    {
        private T[] m_Buffer;
        private int m_Count;

        public T[] Buffer => m_Buffer;
        public T this[int index]
        {
            get => m_Buffer[index];
            set
            {
                m_Buffer[index] = value;
            }
        }
        /// <summary>
        /// 현재 배열에 실제 담긴 객체의 개수입니다.
        /// </summary>
        public int Count => m_Count;
        /// <summary>
        /// 현재 배열의 최대 길이입니다.
        /// </summary>
        public int Length => m_Buffer.Length;

        public FixedList()
        {
            m_Buffer = Array.Empty<T>();
            m_Count = 0;
        }
        public FixedList(T[] buffer)
        {
            m_Buffer = buffer;
            m_Count = buffer.Length;
        }

        public ref T ElementAt(int index)
        {
            return ref m_Buffer[index];
        }
        public void Add(T element)
        {
            if (m_Buffer.Length <= m_Count)
            {
                Array.Resize(ref m_Buffer, m_Count + 1);
            }

            m_Buffer[m_Count] = element;
            m_Count++;
        }
        /// <summary>
        /// 해당 객체를 이 배열에서 제거하고, 재배열을 피하기 위해 해당 객체 뒤의 객체 배열을 앞으로 당깁니다.
        /// </summary>
        /// <param name="element"></param>
        public void RemoveSwapback(T element)
        {
            if (!m_Buffer.RemoveForSwapBack(element))
            {
                return;
            }

            m_Count--;
        }
        public void RemoveAtSwapback(int index)
        {
#if DEBUG_MODE
            if (index < 0)
            {
                throw new IndexOutOfRangeException();
            }
#endif
            for (int i = index + 1; i < m_Buffer.Length; i++)
            {
                m_Buffer[i - 1] = m_Buffer[i];
            }
        }
    }
}
