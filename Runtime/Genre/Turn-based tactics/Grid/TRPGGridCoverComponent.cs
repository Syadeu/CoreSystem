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
using System;
using Unity.Collections;

namespace Syadeu.Presentation.TurnTable
{
    public struct TRPGGridCoverComponent : IEntityComponent
    {
        [BurstCompatible]
        public struct Dimension
        {
            public Direction direction;
            public int forwardLength;
        }
        [BurstCompatible]
        public struct Dimension4
        {
            public Dimension
                a, b, c, d;

            public Dimension this[Direction dir]
            {
                get
                {
                    return dir switch
                    {
                        Direction.Left => a,
                        Direction.Right => b,
                        Direction.Forward => c,
                        Direction.Backward => d,
                        _ => throw new IndexOutOfRangeException($"{dir}"),
                    };
                }
                set
                {
                    switch (dir)
                    {
                        case Direction.Left: a = value; break;
                        case Direction.Right: b = value; break;
                        case Direction.Forward: c = value; break;
                        case Direction.Backward: d = value; break;
                        default:
                            throw new IndexOutOfRangeException($"{dir}");
                    }
                }
            }
        }

        public bool immutable;
        public Dimension4 dimensions;
    }
}