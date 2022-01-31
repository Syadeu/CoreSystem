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

using Syadeu.Collections;
using Syadeu.Presentation.Grid.LowLevel;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    [BurstCompatible]
    public readonly struct GridIndex : IEmpty, IEquatable<GridIndex>
    {
        internal readonly short m_CheckSum;
        private readonly ulong m_Index;

        public ulong Index => m_Index;
        public int3 Location
        {
            get
            {
                int3 location;
                unsafe
                {
                    BurstGridMathematics.indexToLocation(in m_Index, &location);
                }
                return location;
            }
        }

        internal GridIndex(WorldGrid grid, ulong index)
        {
            m_CheckSum = grid.m_CheckSum;
            m_Index = index;
        }
        internal GridIndex(short checkSum, ulong index)
        {
            m_CheckSum = checkSum;
            m_Index = index;
        }
        internal GridIndex(short checkSum, int3 location)
        {
            m_CheckSum = checkSum;
            ulong temp;
            unsafe
            {
                BurstGridMathematics.locationToIndex(location, &temp);
            }
            m_Index = temp;
        }

        //public GridIndex GetDirection(in Direction direction)
        //{
        //    int3 location = Location;
        //    if ((direction & Direction.Up) == Direction.Up)
        //    {
        //        location.y += 1;
        //    }
        //    if ((direction & Direction.Down) == Direction.Down)
        //    {
        //        location.y -= 1;
        //    }
        //    if ((direction & Direction.Left) == Direction.Left)
        //    {
        //        location.x -= 1;
        //    }
        //    if ((direction & Direction.Right) == Direction.Right)
        //    {
        //        location.x += 1;
        //    }
        //    if ((direction & Direction.Forward) == Direction.Forward)
        //    {
        //        location.z += 1;
        //    }
        //    if ((direction & Direction.Forward) == Direction.Backward)
        //    {
        //        location.z -= 1;
        //    }
        //    return new GridIndex(m_CheckSum, location);
        //}

        public bool Equals(GridIndex other) => m_Index.Equals(other.m_Index) && m_CheckSum.Equals(m_CheckSum);

        public bool IsEmpty() => m_CheckSum == 0 && m_Index == 0;

        [NotBurstCompatible]
        public override string ToString() => $"{m_Index}({Location})";
    }
    public struct GridBlock
    {
        public int3 GridIndex;

        // 0 Left
        // 1 Right
        // 2 Down (Forward)
        // 3 Up (Backward)
        public bool4 HasBlocks;
        public float3x2 Vertices0, Vertices1, Vertices2, Vertices3;
    }

    public struct GridIndex4 : IFixedList<GridIndex>
    {
        private GridIndex a, b, c, d;

        public GridIndex this[int index]
        {
            get
            {
                return index switch
                {
                    0 => a,
                    1 => b,
                    2 => c,
                    3 => d,
                    _ => throw new Exception(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        a = value;
                        break;
                    case 1:
                        b = value;
                        break;
                    case 2:
                        c = value;
                        break;
                    case 3:
                        d = value;
                        break;
                    default:
                        throw new Exception();
                }
            }
        }

        public GridIndex First => a;
        public GridIndex Last => d;

        public int Length => throw new NotImplementedException();
    }
}
