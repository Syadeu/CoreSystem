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

using Syadeu.Collections;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    public struct GridSizeComponent : IEntityComponent, IDisposable
    {
        //internal EntityData<IEntityData> m_Parent;

        internal GridLayerChain m_ObstacleLayers;
        //internal UnsafeHashSet<int> m_ObstacleLayerIndicesHashSet;
        //internal GridLayerChain m_ObstacleLayers;
        public FixedList512Bytes<GridPosition> positions;

        public float CellSize => PresentationSystem<DefaultPresentationGroup, GridSystem>.System.CellSize;

        void IDisposable.Dispose()
        {
            //m_ObstacleLayerIndicesHashSet.Dispose();
        }

        public float3 IndexToPosition(in int index)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.IndexToPosition(index);
        }

        public bool IsMyIndex(int index)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i].index == index) return true;
            }
            return false;
        }

        [Obsolete("Use GetRange Instead")]
        public int[] GetRange(int range, params int[] ignoreLayers)
        {
            GridSystem grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;

            int[] indices = grid.GetRange(positions[0].index, range, ignoreLayers);
            return indices;
        }

        /// <summary>
        /// <see cref="GridSizeAttribute.m_ObstacleLayers"/> 에서 지정한 레이어를 기반으로,
        /// <paramref name="range"/> 범위 만큼 반환합니다.
        /// </summary>
        /// <remarks>
        /// <paramref name="list"/> 는 자동으로 Clear 됩니다. 
        /// 직접 레이어를 지정하고 싶으면 
        /// <seealso cref="GetRange(ref NativeList{int}, in int, in FixedList128Bytes{int})"/> 를 사용하세요.
        /// </remarks>
        /// <param name="list"></param>
        /// <param name="range"></param>
        public void GetRange(ref NativeList<int> list, in int range)
        {
            int bufferLength = CalculateMaxiumIndicesInRangeCount(in range);

            unsafe
            {
                int* buffer = stackalloc int[bufferLength];
                GetRange(in buffer, in bufferLength, in range, out int count);

                list.Clear();
                list.AddRange(buffer, count);
            }
        }
        public void GetRange(ref NativeList<int> list, in int range, in GridLayerChain ignoreLayers)
        {
            int bufferLength = CalculateMaxiumIndicesInRangeCount(in range);

            unsafe
            {
                int* buffer = stackalloc int[bufferLength];
                GetRange(in buffer, in bufferLength, in range, in ignoreLayers, out int count);

                list.Clear();
                list.AddRange(buffer, count);
            }
        }
        public void GetRange(ref FixedList4096Bytes<int> list, in int range)
        {
            int bufferLength = CalculateMaxiumIndicesInRangeCount(in range);

            unsafe
            {
                int* buffer = stackalloc int[bufferLength];
                GetRange(in buffer, bufferLength, in range, out int count);

                list.Clear();
                list.AddRange(buffer, count);
            }
        }
        unsafe public void GetRange(in int* buffer, in int bufferSize, in int range, out int count)
        {
            GridSystem grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;

            grid.GetRange(in buffer, in bufferSize, positions[0].index, in range, in m_ObstacleLayers, out count);
        }
        unsafe public void GetRange(in int* buffer, in int bufferSize, in int range, in GridLayerChain ignoreLayers, out int count)
        {
            GridSystem grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;

            grid.GetRange(in buffer, in bufferSize, positions[0].index, in range, in ignoreLayers, out count);
        }

        public bool HasPath(int to, in int maxIteration = 32) => HasPath(in to, out _, maxIteration);
        public bool HasPath(in int to, out int pathCount, in int maxIteration = 32)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.HasPath(
                positions[0].index, 
                in to, 
                out pathCount,
                in m_ObstacleLayers,
                default,
                in maxIteration,
                avoidEntity: true);
        }

        public bool HasDirection(GridPosition position, Direction direction, out GridPosition target)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.HasDirection(in position.index, in direction, out target);
        }

        public bool GetPath64(in int to, ref GridPath64 path, in int maxIteration = 32, in bool avoidEntity = true)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.GetPath64(
                positions[0].index, 
                in to, 
                ref path,
                in m_ObstacleLayers, 
                default,
                in maxIteration,
                in avoidEntity);
        }
        public bool GetPath64(in int to, ref GridPath64 path, FixedList512Bytes<int> additionalIgnoreIndices, in int maxIteration = 32, in bool avoidEntity = true)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.GetPath64(
                positions[0].index,
                in to,
                ref path,
                in m_ObstacleLayers,
                additionalIgnoreIndices,
                in maxIteration,
                in avoidEntity);
        }
        public bool GetPath64(in int to, ref GridPath64 path, in GridLayerChain ignoreLayers, in int maxIteration = 32, in bool avoidEntity = true)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.GetPath64(
                positions[0].index,
                in to,
                ref path,
                m_ObstacleLayers.Combine(in ignoreLayers),
                default,
                in maxIteration,
                in avoidEntity);
        }
        public bool GetPath64(in int to, ref GridPath64 path, in GridLayerChain ignoreLayers, FixedList512Bytes<int> additionalIgnoreIndices, in int maxIteration = 32, in bool avoidEntity = true)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.GetPath64(
                positions[0].index,
                in to,
                ref path,
                m_ObstacleLayers.Combine(in ignoreLayers),
                additionalIgnoreIndices,
                in maxIteration,
                in avoidEntity);
        }

        public GridPosition GetGridPosition(in int index)
        {
            return new GridPosition(index, PresentationSystem<DefaultPresentationGroup, GridSystem>.System.IndexToLocation(index));
        }

        public static int CalculateMaxiumIndicesInRangeCount(in int range)
        {
            int height = ((range * 2) + 1);
            return height * height;
        }
    }
}
