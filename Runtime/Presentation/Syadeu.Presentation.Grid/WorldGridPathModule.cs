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
        private struct PathTile : IEmpty, IEquatable<GridIndex>
        {
            public GridIndex parent, index;
            /// <summary>
            /// 부모를 기준으로 이 타일이 위치한 방향입니다.
            /// </summary>
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

            public bool IsSameAxis(in GridIndex a, in GridIndex b)
            {
                int3
                    location = index.Location,
                    targetA = a.Location,
                    targetB = b.Location;

                if ((location.y == targetA.y && location.y == targetB.y))
                {
                    return
                        (location.x == targetA.x && location.x == targetB.x) ||
                        (location.z == targetA.z && location.z == targetB.z);
                }
                else
                {
                    return
                        (location.x == targetA.x && location.x == targetB.x) &&
                        (location.z == targetA.z && location.z == targetB.z);
                }
            }

            public bool Equals(GridIndex other) => index.Equals(other);
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

            UnsafeFixedListWrapper<GridIndex> fourList;
            UnsafeFixedListWrapper<PathTile> pathList;
            unsafe
            {
                GridIndex* four = stackalloc GridIndex[6];
                for (int i = 0; i < 6; i++)
                {
                    System.TryGetDirection(from, (Direction)(1 << i), out GridIndex result);
                    four[i] = result;
                }

                fourList = new UnsafeFixedListWrapper<GridIndex>(
                    four, 6, 6);
            }

            unsafe
            {
                PathTile* path = stackalloc PathTile[512];
                pathList = new UnsafeFixedListWrapper<PathTile>(
                    path, 512);
            }

            pathList.AddNoResize(root);
            //path[0] = root;

            pathFound = 1;
            uint /*count = 1,*/ iteration = 0, currentTileIdx = 0;

            while (
                iteration < maxIteration &&
                pathList.Length < pathList.Capacity &&
                pathList.Last.index.Index != to.Index)
            {
                ref PathTile lastTileData = ref pathList.ElementAt(currentTileIdx);

                Direction nextDirection = GetLowestCost(ref lastTileData, in to, out GridIndex result);
                if (nextDirection < 0)
                {
                    pathFound--;

                    if (pathFound <= 0) break;

                    ref PathTile parentTile = ref pathList.ElementAt(lastTileData.parentArrayIdx);
                    parentTile.SetClose(lastTileData.direction, true);

                    currentTileIdx = lastTileData.parentArrayIdx;

                    iteration++;
                    continue;
                }

                PathTile nextTile = GetOrCreateNext(pathList, lastTileData, result, nextDirection, out bool isNew);

                lastTileData.SetClose(nextDirection, true);
                CalculatePathTile(ref nextTile);

                if (isNew)
                {
                    currentTileIdx = (uint)pathList.Length;
                    pathList.AddNoResize(nextTile);
                    //count++;
                }
                else
                {
                    currentTileIdx = nextTile.arrayIdx;
                }

                pathFound++;
            }

            // Path Found
            if (pathList.Last.Equals(to))
            {
                for (int i = 0; i < pathList.Length; i++)
                {
                    if (fourList.Contains(pathList[i].index))
                    {
                        ref var target = ref pathList.ElementAt(i);
                        target.parent = from;
                        target.parentArrayIdx = 0;
                    }
                    //if (UnsafeBufferUtility.Contains(four, 6, path[i].index))
                    //{
                    //    path[i].parent = from;
                    //    path[i].parentArrayIdx = 0;
                    //}
                }

                int sortedFound = 0;
                PathTile current = pathList.Last;
                for (int i = 0; i < pathFound && !current.index.Equals(from); i++, sortedFound++)
                {
                    current = pathList[current.parentArrayIdx];
                }

                pathFound = sortedFound;

                return true;
            }

            return false;
        }
        [SkipLocalsInit]
        public bool GetPath(
            in GridIndex from,
            in GridIndex to,
            ref FixedList4096Bytes<GridIndex> foundPath,
            out int tileCount,
            in int maxIteration = 32
            )
        {
            PathTile root = new PathTile(from);
            CalculatePathTile(ref root);

            UnsafeFixedListWrapper<GridIndex> fourList;
            UnsafeFixedListWrapper<PathTile> pathList;
            unsafe
            {
                GridIndex* four = stackalloc GridIndex[6];
                for (int i = 0; i < 6; i++)
                {
                    System.TryGetDirection(from, (Direction)(1 << i), out GridIndex result);
                    four[i] = result;
                }
                fourList = new UnsafeFixedListWrapper<GridIndex>(
                    four, 6, 6);

                PathTile* path = stackalloc PathTile[512];
                pathList = new UnsafeFixedListWrapper<PathTile>(
                    path, 512);
            }
            pathList.AddNoResize(root);

            int pathFound = 1;
            uint /*count = 1, */iteration = 0, currentTileIdx = 0;

            while (
                iteration < maxIteration &&
                pathList.Length < pathList.Capacity &&
                pathList.Last.index.Index != to.Index)
            {
                ref PathTile lastTileData = ref pathList.ElementAt(currentTileIdx);

                Direction nextDirection = GetLowestCost(ref lastTileData, in to, out GridIndex result);
                if (nextDirection == Direction.NONE)
                {
                    pathFound--;

                    if (pathFound <= 0) break;

                    ref PathTile parentTile = ref pathList.ElementAt(lastTileData.parentArrayIdx);
                    parentTile.SetClose(lastTileData.direction, true);

                    currentTileIdx = lastTileData.parentArrayIdx;

                    iteration++;
                    continue;
                }

                PathTile nextTile = GetOrCreateNext(pathList, lastTileData, result, nextDirection, out bool isNew);

                lastTileData.SetClose(nextDirection, true);
                CalculatePathTile(ref nextTile);

                if (isNew)
                {
                    currentTileIdx = (uint)pathList.Length;
                    pathList.AddNoResize(nextTile);
                }
                else
                {
                    currentTileIdx = nextTile.arrayIdx;
                }

                pathFound++;
            }

            // Path Found
            if (pathList.Last.index.Equals(to))
            {
                UnsafeFixedListWrapper<PathTile> output;
                unsafe
                {
                    PathTile* arr = stackalloc PathTile[pathFound];
                    output = new UnsafeFixedListWrapper<PathTile>(arr, pathFound);
                }

                PathTile current = pathList.Last;
                for (int i = 0; i < pathFound && !current.index.Equals(from); i++)
                {
                    if (fourList.Contains(pathList[i].index))
                    {
                        ref var target = ref pathList.ElementAt(i);
                        target.parent = from;
                        target.parentArrayIdx = 0;
                    }
                    output.AddNoResize(current);

                    current = pathList[current.parentArrayIdx];
                }
                //arr[length++] = path[0];
                output.AddNoResize(pathList[0]);

                foundPath.Clear();
                //foundPath.Add(from);
                for (int i = output.Length - 1; i >= 0; i--)
                {
                    if (i + 1 < output.Length && i - 1 >= 0)
                    {
                        if (output[i].IsSameAxis(output[i + 1].index, output[i - 1].index))
                        {
                            continue;
                        }
                    }

                    foundPath.Add(output[i].index);
                }

                tileCount = output.Length;
                return true;
            }

            tileCount = 0;
            return false;
        }

        private unsafe PathTile GetOrCreateNext(UnsafeFixedListWrapper<PathTile> array,
            in PathTile from, in GridIndex target, in Direction targetDirection, out bool isNew)
        {
            int index = array.IndexOf(target);
            if (index < 0)
            {
                isNew = true;
                return new PathTile(from, target, targetDirection, (uint)array.Length);
            }

            isNew = false;
            return array[index];

            //for (int i = 0; i < array.Count; i++)
            //{
            //    if (array[i].index.Equals(target))
            //    {
            //        isNew = false;
            //        return array[i];
            //    }
            //}

            //isNew = true;
            //return new PathTile(from, target, targetDirection, length);
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
                    foreach (var item in iter)
                    {
                        GridComponent gridComponent = item.GetComponent<GridComponent>();
                        isBlock |= (gridComponent.ObstacleType & GridComponent.Obstacle.Block) == GridComponent.Obstacle.Block;
                    }

                    tile.closed[i] = isBlock;
                    continue;
                }

                tile.closed[i] = false;
            }
        }
    }
}
