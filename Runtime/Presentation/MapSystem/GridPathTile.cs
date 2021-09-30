using Syadeu.Database;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    [BurstCompatible]
    internal struct GridPathTile : IEquatable<GridPathTile>
    {
        public static readonly GridPathTile Empty = new GridPathTile(-1, -1);

        public GridPosition parent;
        public int direction;
        public GridPosition position;

        public bool4 opened;
        public GridPosition4 openedPositions;
        private int4 costs;

        public GridPathTile(int index, int2 location)
        {
            this.parent = GridPosition.Empty;
            this.direction = -1;
            this.position = new GridPosition(index, location);

            opened = false;
            openedPositions = GridPosition4.Empty;
            costs = -1;
        }
        public GridPathTile(GridPosition parent, GridPosition position, int direction)
        {
            this.parent = parent;
            this.direction = direction;
            this.position = position;

            opened = false;
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
        public GridPathTile GetNext(int direction)
        {
            return new GridPathTile(position, openedPositions[direction], direction);
        }
        public bool IsRoot() => parent.IsEmpty();
        public bool IsBlocked()
        {
            for (int i = 0; i < 4; i++)
            {
                if (opened[i]) return false;
            }
            return true;
        }
        public void Calculate(in BinaryGrid grid, in NativeHashSet<int> ignoreLayers = default)
        {
            for (int i = 0; i < 4; i++)
            {
                int2 nextTempLocation = grid.GetDirection(in position.location, (Direction)(1 << i));
                if (nextTempLocation.Equals(parent.location)) continue;

                //int nextTemp = GridBurstExtensions.p_LocationInt2ToIndex.Invoke(grid.bounds, grid.cellSize, nextTempLocation);
                int nextTemp = grid.LocationToIndex(nextTempLocation);
                if (ignoreLayers.IsCreated)
                {
                    if (ignoreLayers.Contains(nextTemp))
                    {
                        opened[i] = false;
                        openedPositions.RemoveAt(i);
                        continue;
                    }
                }

                opened[i] = true;
                openedPositions.UpdateAt(i, nextTemp, nextTempLocation);
            }
        }
        public void Calculate(in BinaryGrid grid, in NativeHashSet<int> ignoreLayers, in NativeHashSet<int> additionalIgnore = default)
        {
            for (int i = 0; i < 4; i++)
            {
                int2 nextTempLocation = grid.GetDirection(in position.location, (Direction)(1 << i));
                if (nextTempLocation.Equals(parent.location)) continue;

                //int nextTemp = GridBurstExtensions.p_LocationInt2ToIndex.Invoke(grid.bounds, grid.cellSize, nextTempLocation);
                int nextTemp = grid.LocationToIndex(nextTempLocation);
                if (ignoreLayers.IsCreated)
                {
                    if (ignoreLayers.Contains(nextTemp))
                    {
                        opened[i] = false;
                        openedPositions.RemoveAt(i);
                        continue;
                    }
                }

                if (additionalIgnore.IsCreated)
                {
                    if (additionalIgnore.Contains(nextTemp))
                    {
                        opened[i] = false;
                        openedPositions.RemoveAt(i);
                        continue;
                    }
                }

                opened[i] = true;
                openedPositions.UpdateAt(i, nextTemp, nextTempLocation);
            }
        }

        public bool IsEmpty() => Equals(Empty);
        public bool Equals(GridPathTile other)
            => parent.Equals(other) && direction.Equals(other.direction) && position.Equals(other.position);
    }
}
