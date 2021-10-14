using Syadeu.Collections;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    /// <summary>
    /// For Pathfinding only
    /// </summary>
    [BurstCompatible]
    internal struct GridPathTile : IEquatable<GridPathTile>
    {
        public static readonly GridPathTile Empty = new GridPathTile(-1, -1, -1, -1);

        public int parentArrayIdx;
        public int arrayIdx;

        public GridPosition parent;
        public int direction;
        public GridPosition position;

        public bool4 opened;
        public GridPosition4 openedPositions;
        private int4 costs;

        public GridPathTile(int parentArrayIdx, int arrayIdx, int index, int2 location)
        {
            this.parentArrayIdx = parentArrayIdx;
            this.arrayIdx = arrayIdx;

            this.parent = GridPosition.Empty;
            this.direction = -1;
            this.position = new GridPosition(index, location);

            opened = true;
            openedPositions = GridPosition4.Empty;
            costs = -1;
        }
        public GridPathTile(int parentArrayIdx, int arrayIdx, GridPosition parent, GridPosition position, int direction)
        {
            this.parentArrayIdx = parentArrayIdx;
            this.arrayIdx = arrayIdx;

            this.parent = parent;
            this.direction = direction;
            this.position = position;

            opened = true;
            openedPositions = GridPosition4.Empty;
            costs = -1;
        }

        public int GetCost(int direction, int2 to)
        {
            if (costs[direction] < 0)
            {
                int2 temp = openedPositions.location[direction] - to;
                costs[direction] = (temp.x * temp.x) + (temp.y * temp.y);
            }

            return costs[direction];
        }
        //public GridPathTile GetNext(in GridMapAttribute grid, int direction)
        //{
        //    openedPositions[direction] = grid.GetDirection(in position.index, (Direction)(1 << direction));

        //    return new GridPathTile(position, openedPositions[direction], direction);
        //}
        public bool IsRoot() => parent.IsEmpty();
        public bool IsBlocked()
        {
            for (int i = 0; i < 4; i++)
            {;
                if (opened[i]) return false;
            }
            return true;
        }
        //public void Calculate(in GridMapAttribute grid, 
        //    in NativeHashSet<int> ignoreLayers = default, in NativeHashSet<int> additionalIgnore = default)
        //{
        //    for (int i = 0; i < 4; i++)
        //    {
        //        GridPosition nextTempLocation = grid.GetDirection(in position.index, (Direction)(1 << i));
        //        if (nextTempLocation.Equals(parent)) continue;

        //        //int nextTemp = GridBurstExtensions.p_LocationInt2ToIndex.Invoke(grid.bounds, grid.cellSize, nextTempLocation);
        //        int nextTemp = nextTempLocation.index;
        //        if (ignoreLayers.IsCreated)
        //        {
        //            if (ignoreLayers.Contains(nextTemp))
        //            {
        //                opened[i] = false;
        //                openedPositions.RemoveAt(i);
        //                continue;
        //            }
        //        }

        //        if (additionalIgnore.IsCreated)
        //        {
        //            if (additionalIgnore.Contains(nextTemp))
        //            {
        //                opened[i] = false;
        //                openedPositions.RemoveAt(i);
        //                continue;
        //            }
        //        }

        //        opened[i] = true;
        //        openedPositions.UpdateAt(i, nextTempLocation);
        //    }
        //}
        //public void Calculate(in BinaryGrid grid, in NativeHashSet<int> ignoreLayers = default, in NativeHashSet<int> additionalIgnore = default)
        //{
        //    for (int i = 0; i < 4; i++)
        //    {
        //        int2 nextTempLocation = grid.GetDirection(in position.location, (Direction)(1 << i));
        //        if (nextTempLocation.Equals(parent.location)) continue;

        //        //int nextTemp = GridBurstExtensions.p_LocationInt2ToIndex.Invoke(grid.bounds, grid.cellSize, nextTempLocation);
        //        int nextTemp = grid.LocationToIndex(nextTempLocation);
        //        if (ignoreLayers.IsCreated)
        //        {
        //            if (ignoreLayers.Contains(nextTemp))
        //            {
        //                opened[i] = false;
        //                openedPositions.RemoveAt(i);
        //                continue;
        //            }
        //        }

        //        if (additionalIgnore.IsCreated)
        //        {
        //            if (additionalIgnore.Contains(nextTemp))
        //            {
        //                opened[i] = false;
        //                openedPositions.RemoveAt(i);
        //                continue;
        //            }
        //        }

        //        opened[i] = true;
        //        openedPositions.UpdateAt(i, nextTemp, nextTempLocation);
        //    }
        //}

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(GridPathTile other)
            => parent.Equals(other) && direction.Equals(other.direction) && position.Equals(other.position);
    }
}
