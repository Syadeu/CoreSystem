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
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    [System.Obsolete("Use WorldGridSystem Instead", true)]
    public struct GridPosition : IGridPosition, IEquatable<GridPosition>
    {
        public static readonly GridPosition Empty = new GridPosition(-1, -1);
        public const int c_Size = 12;

        public int index;
        public int2 location;

        public int Length => 1;

        public int2 this[int i]
        {
            get => location;
        }

        public GridPosition(int index, int2 location)
        {
            this.index = index;
            this.location = location;
        }

        public void Update(int index, int2 location)
        {
            this.index = index;
            this.location = location;
        }

        public bool IsEmpty() => index == -1 && location.Equals(-1);
        public bool IsValid() => !Equals(Empty);
        public bool Equals(IGridPosition other)
        {
            if (!Length.Equals(other.Length) ||
                !this[0].Equals(other[0])) return false;

            return true;
        }

        public bool Equals(GridPosition other) => index.Equals(other.index) && location.Equals(other.location);
        public override int GetHashCode() => index;
    }
}
