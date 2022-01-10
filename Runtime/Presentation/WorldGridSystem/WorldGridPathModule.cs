﻿// Copyright 2021 Seung Ha Kim
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
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Presentation.Components;
using System;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    internal sealed class WorldGridPathModule : PresentationSystemModule<WorldGridSystem>
    {
        private struct PathTile : IEmpty
        {
            public GridIndex parent, index;
            public Direction direction;
            public uint parentArrayIdx, arrayIdx;

            // TODO : 더 작은걸로 6
            public ClosedBoolen6 closed;

            public PathTile(GridIndex index)
            {
                this = default(PathTile);

                this.index = index;
            }
            public PathTile(PathTile parent, GridIndex index, Direction direction, uint arrayIdx)
            {
                this = default(PathTile);

                this.parent = parent.index;
                this.index = index;
                this.direction = direction;
                parentArrayIdx = parent.arrayIdx;
                this.arrayIdx = arrayIdx;
            }

            public void SetClose(in Direction direction, bool value)
            {
                if ((direction & Direction.Up) == Direction.Up)
                {
                    closed[0] = value;
                }
                if ((direction & Direction.Down) == Direction.Down)
                {
                    closed[1] = value;
                }
                if ((direction & Direction.Left) == Direction.Left)
                {
                    closed[2] = value;
                }
                if ((direction & Direction.Right) == Direction.Right)
                {
                    closed[3] = value;
                }
                if ((direction & Direction.Forward) == Direction.Forward)
                {
                    closed[4] = value;
                }
                if ((direction & Direction.Backward) == Direction.Backward)
                {
                    closed[5] = value;
                }
            }
            public bool IsRoot() => parent.IsEmpty();

            public bool IsEmpty()
            {
                return parent.IsEmpty() && index.IsEmpty();
            }
        }
        private struct ClosedBoolen6
        {
            private bool
                a0, a1, a2,
                b0, b1, b2;

            public bool this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => a0,
                        1 => a1,
                        2 => a2,
                        3 => b0,
                        4 => b1,
                        5 => b2,
                        _ => throw new IndexOutOfRangeException()
                    };
                }
                set
                {
                    if (index == 0) a0 = value;
                    else if (index == 1) a1 = value;
                    else if (index == 2) a2 = value;
                    else if (index == 3) b0 = value;
                    else if (index == 4) b1 = value;
                    else if (index == 5) b2 = value;
                }
            }
        }

        [SkipLocalsInit]
        public bool HasPath(
            in GridIndex from,
            in GridIndex to,
            out int pathFound,
            in int maxIteration = 32
            )
        {
            PathTile root = new PathTile(from);
            CalculatePathTile(ref root);
            unsafe
            {
                GridIndex* four = stackalloc GridIndex[6];
                for (int i = 0; i < 6; i++)
                {
                    System.TryGetDirection(from, (Direction)(1 << i), out GridIndex result);
                    four[i] = result;
                }

                PathTile* path = stackalloc PathTile[512];
                path[0] = root;

                pathFound = 1;
                uint count = 1, iteration = 0, currentTileIdx = 0;

                while (
                    iteration < maxIteration &&
                    count < 512 &&
                    path[count - 1].index.Index != to.Index)
                {
                    ref PathTile lastTileData = ref path[currentTileIdx];

                    Direction nextDirection = GetLowestCost(ref lastTileData, in to, out GridIndex result);
                    if (nextDirection < 0)
                    {
                        pathFound--;

                        if (pathFound <= 0) break;

                        ref PathTile parentTile = ref path[lastTileData.parentArrayIdx];
                        parentTile.SetClose(lastTileData.direction, true);

                        currentTileIdx = lastTileData.parentArrayIdx;

                        iteration++;
                        continue;
                    }

                    PathTile nextTile = GetOrCreateNext(path, count, lastTileData, result, nextDirection, out bool isNew);

                    lastTileData.SetClose(nextDirection, true);
                    CalculatePathTile(ref nextTile);

                    if (isNew)
                    {
                        path[count] = (nextTile);
                        currentTileIdx = count;
                        count++;
                    }
                    else
                    {
                        currentTileIdx = nextTile.arrayIdx;
                    }

                    pathFound++;
                }

                // Path Found
                if (path[count - 1].index.Equals(to))
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (UnsafeBufferUtility.Contains(four, 6, path[i].index))   
                        {
                            path[i].parent = from;
                            path[i].parentArrayIdx = 0;
                        }
                    }

                    int sortedFound = 0;
                    PathTile current = path[count - 1];
                    for (int i = 0; i < pathFound && !current.index.Equals(from); i++, sortedFound++)
                    {
                        current = path[current.parentArrayIdx];
                    }

                    pathFound = sortedFound;

                    return true;
                }
            }

            return false;
        }
        [SkipLocalsInit]
        public bool GetPath(
            in GridIndex from,
            in GridIndex to,
            ref FixedList4096Bytes<GridIndex> foundPath,
            in int maxIteration = 32
            )
        {
            PathTile root = new PathTile(from);
            CalculatePathTile(ref root);
            unsafe
            {
                GridIndex* four = stackalloc GridIndex[6];
                for (int i = 0; i < 6; i++)
                {
                    System.TryGetDirection(from, (Direction)(1 << i), out GridIndex result);
                    four[i] = result;
                }

                PathTile* path = stackalloc PathTile[512];
                path[0] = root;

                int pathFound = 1;
                uint count = 1, iteration = 0, currentTileIdx = 0;

                while (
                    iteration < maxIteration &&
                    count < 512 &&
                    path[count - 1].index.Index != to.Index)
                {
                    ref PathTile lastTileData = ref path[currentTileIdx];

                    Direction nextDirection = GetLowestCost(ref lastTileData, in to, out GridIndex result);
                    if (nextDirection == Direction.NONE)
                    {
                        pathFound--;

                        if (pathFound <= 0) break;

                        ref PathTile parentTile = ref path[lastTileData.parentArrayIdx];
                        parentTile.SetClose(lastTileData.direction, true);

                        currentTileIdx = lastTileData.parentArrayIdx;

                        iteration++;
                        continue;
                    }

                    PathTile nextTile = GetOrCreateNext(path, count, lastTileData, result, nextDirection, out bool isNew);

                    lastTileData.SetClose(nextDirection, true);
                    CalculatePathTile(ref nextTile);

                    if (isNew)
                    {
                        path[count] = (nextTile);
                        currentTileIdx = count;
                        count++;
                    }
                    else
                    {
                        currentTileIdx = nextTile.arrayIdx;
                    }

                    pathFound++;
                }

                // Path Found
                if (path[count - 1].index.Equals(to))
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (UnsafeBufferUtility.Contains(four, 6, path[i].index))
                        {
                            path[i].parent = from;
                            path[i].parentArrayIdx = 0;
                        }
                    }

                    GridIndex* arr = stackalloc GridIndex[pathFound];

                    int length = 0;
                    PathTile current = path[count - 1];
                    for (int i = 0; i < pathFound && !current.index.Equals(from); i++, length++)
                    {
                        arr[i] = current.index;

                        current = path[current.parentArrayIdx];
                    }

                    foundPath.Clear();
                    foundPath.Add(from);
                    for (int i = length - 1; i >= 0; i--)
                    {
                        foundPath.Add(arr[i]);
                    }

                    return true;
                }
            }

            return false;
        }

        private unsafe PathTile GetOrCreateNext(PathTile* array, in uint length,
            in PathTile from, in GridIndex target, in Direction targetDirection, out bool isNew)
        {
            for (int i = 0; i < length; i++)
            {
                if (array[i].index.Equals(target))
                {
                    isNew = false;
                    return array[i];
                }
            }

            isNew = true;
            return new PathTile(from, target, targetDirection, length);
        }
        private Direction GetLowestCost(ref PathTile prev, in GridIndex to, out GridIndex result)
        {
            Direction lowest = 0;
            result = default(GridIndex);
            int cost = int.MaxValue;

            for (int i = 0; i < 6; i++)
            {
                if (prev.closed[i]) continue;

                Direction direction = (Direction)(1 << i);
                if (!System.TryGetDirection(prev.index, direction, out GridIndex index))
                {
                    prev.closed[i] = true;
                    continue;
                }

                int3 dir = index.Location - to.Location;
                int tempCost = (dir.x * dir.x) + (dir.y * dir.y) + (dir.z * dir.z);
                if (direction == Direction.Forward || direction == Direction.Backward)
                {
                    tempCost += 1;
                }

                if (tempCost < cost)
                {
                    lowest = (Direction)(1 << i);
                    result = index;
                    cost = tempCost;
                }
            }

            return lowest;
        }
        private void CalculatePathTile(ref PathTile tile)
        {
            for (int i = 0; i < 6; i++)
            {
                if (!System.TryGetDirection(tile.index, (Direction)(1 << i), out GridIndex target))
                {
                    tile.closed[i] = true;
                    continue;
                }
                
                if (System.TryGetEntitiesAt(in target, out var iter))
                {
                    bool isBlock = false;
                    using (iter)
                    {
                        while (iter.MoveNext())
                        {
                            GridComponent gridComponent = iter.Current.GetComponent<GridComponent>();
                            isBlock |= gridComponent.ObstacleType == GridComponent.Obstacle.Block;
                        }
                    }

                    tile.closed[i] = isBlock;
                    continue;
                }

                tile.closed[i] = false;
            }
        }
    }
}
