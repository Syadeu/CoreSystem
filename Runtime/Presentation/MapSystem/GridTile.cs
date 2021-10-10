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
