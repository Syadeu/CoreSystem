// Copyright 2022 Seung Ha Kim
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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Collections
{
    [Serializable]
    public sealed class LinkedBlock
    {
        [Serializable]
        public sealed class Column
        {
            [SerializeField, JsonProperty(Order = 0, PropertyName = "Positions")]
            private bool[] m_Positions = Array.Empty<bool>();

            [JsonIgnore] public bool[] Positions => m_Positions;
        }

        [SerializeField, JsonProperty(Order = 0, PropertyName = "Columns")]
        private Column[] m_Columns = Array.Empty<Column>();

        [JsonIgnore] public IReadOnlyList<Column> Columns => m_Columns;

        [JsonIgnore] public int RowCount => m_Columns.Length;
        [JsonIgnore] public int ColumnCount => m_Columns.Length > 0 ? m_Columns[0].Positions.Length : 0;
        [JsonIgnore] public int Count => RowCount * ColumnCount;

        [JsonIgnore]
        public bool this[int x, int y]
        {
            get
            {
                return m_Columns[x].Positions[y];
            }
            set
            {
                m_Columns[x].Positions[y] = value;
            }
        }
    }
    [BurstCompatible]
    public struct UnsafeLinkedBlock : IDisposable, INativeDisposable
    {
        public struct Column : IDisposable, INativeDisposable
        {
            private unsafe struct Data
            {
                [MarshalAs(UnmanagedType.U1)]
                public bool value;
                public UnsafeReference userData;
            }
            private UnsafeAllocator<Data> m_Positions;

            public bool this[int index]
            {
                get => m_Positions[index].value;
                set => m_Positions[index].value = value;
            }
            public int Length => m_Positions.Length;

            public Column(bool[] pos, Allocator allocator)
            {
                m_Positions = new UnsafeAllocator<Data>(pos.Length, allocator);
                for (int i = 0; i < pos.Length; i++)
                {
                    m_Positions[i] = new Data
                    {
                        value = pos[i]
                    };
                }
            }

            public unsafe void SetUserData(int index, UnsafeReference ptr)
            {
                m_Positions[index].userData = ptr;
            }

            public void Dispose()
            {
                m_Positions.Dispose();
            }
            public JobHandle Dispose(JobHandle inputDeps)
            {
                return m_Positions.Dispose(inputDeps);
            }
        }

        private UnsafeAllocator<Column> m_Columns;

        public Column this[int x]
        {
            get => m_Columns[x];
        }
        public bool this[int x, int y]
        {
            get
            {
                return m_Columns[y][x];
            }
            set
            {
                m_Columns[y][x] = value;
            }
        }
        public int RowLength => m_Columns.Length;

        public UnsafeLinkedBlock(LinkedBlock linkedBlock, Allocator allocator)
        {
            m_Columns = new UnsafeAllocator<Column>(linkedBlock.RowCount, allocator);
            for (int i = 0; i < linkedBlock.RowCount; i++)
            {
                m_Columns[i] = new Column(linkedBlock.Columns[i].Positions, allocator);
            }
        }

        public void SetValue(int2 pos, UnsafeLinkedBlock block, bool value, UnsafeReference userData)
        {
            for (int y = 0; y < block.RowLength; y++)
            {
                for (int x = 0; x < block[y].Length; x++)
                {
                    if (!block[x, y]) continue;

                    int2 reletivePos = pos + new int2(x, y);

                    this[reletivePos.x, reletivePos.y] = value;
                    this[reletivePos.y].SetUserData(reletivePos.x, userData);
                }
            }
        }
        public bool HasSpaceFor(UnsafeLinkedBlock block, out int2 pos)
        {
            pos = int2.zero;
            for (int y = 0; y < RowLength; y++)
            {
                for (int x = 0; x < this[y].Length; x++)
                {
                    pos = new int2(x, y);

                    if (!this[y][x]) continue;
                    else if (HasSpaceFor(pos, block))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public bool HasSpaceFor(int2 pos, UnsafeLinkedBlock block)
        {
            if (!this[pos.x, pos.y]) return false;

            for (int y = 0; y < block.RowLength; y++)
            {
                for (int x = 0; x < block[y].Length; x++)
                {
                    if (!block[y][x]) continue;

                    int2 reletivePos = pos + new int2(x, y);
                    if (!this[reletivePos.x, reletivePos.y]) return false;
                }
            }
            return true;
        }

        public void Dispose()
        {
            for (int i = 0; i < m_Columns.Length; i++)
            {
                m_Columns[i].Dispose();
            }
            m_Columns.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            for (int i = 0; i < m_Columns.Length; i++)
            {
                inputDeps = m_Columns[i].Dispose(inputDeps);
            }
            inputDeps = m_Columns.Dispose(inputDeps);
            return inputDeps;
        }
    }
}
