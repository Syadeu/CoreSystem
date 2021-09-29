using System;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    public struct GridPosition : IGridPosition, IEquatable<GridPosition>
    {
        public static readonly GridPosition Empty = new GridPosition(-1, -1);

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
    }
}
