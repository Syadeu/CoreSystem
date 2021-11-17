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

using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    [BurstCompatible]
    public struct GridTile : IEquatable<GridTile>
    {
        public static GridTile Empty => new GridTile(-1);

        public int parent;
        public int index;

        public GridTile(int2 indices)
        {
            parent = indices.x;
            index = indices.y;
        }

        public bool IsEmpty() => Equals(Empty);

        public bool Equals(GridTile other) => parent == other.parent && index == other.index;
        public override int GetHashCode() => index;
    }
}
