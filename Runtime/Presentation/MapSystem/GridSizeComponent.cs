using Syadeu.Collections;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    public struct GridSizeComponent : IEntityComponent
    {
        //internal PresentationSystemID<GridSystem> m_GridSystem;
        internal EntityData<IEntityData> m_Parent;

        internal FixedList128Bytes<int> m_ObstacleLayers;
        public FixedList512Bytes<GridPosition> positions;

        public float CellSize => PresentationSystem<DefaultPresentationGroup, GridSystem>.System.CellSize;

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
            //GridSystem grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;
            //grid.GetRange(ref list, positions[0].index, in range, m_ObstacleLayers);

            int 
                height = ((range * 2) + 1),
                bufferSize = height * height;
            unsafe
            {
                int* buffer = stackalloc int[bufferSize];
                GetRange(in buffer, in bufferSize, in range, out int count);

                list.Clear();
                list.AddRange(buffer, count);
            }
        }
        public void GetRange(ref NativeList<int> list, in int range, in FixedList128Bytes<int> ignoreLayers)
        {
            //GridSystem grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;
            //grid.GetRange(ref list, positions[0].index, in range, ignoreLayers);

            int
                height = ((range * 2) + 1),
                bufferSize = height * height;
            unsafe
            {
                int* buffer = stackalloc int[bufferSize];
                GetRange(in buffer, in bufferSize, in range, in ignoreLayers, out int count);

                list.Clear();
                list.AddRange(buffer, count);
            }
        }
        unsafe public void GetRange(in int* buffer, in int bufferSize, in int range, out int count)
        {
            GridSystem grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;

            grid.GetRange(in buffer, in bufferSize, positions[0].index, in range,in m_ObstacleLayers, out count);
        }
        unsafe public void GetRange(in int* buffer, in int bufferSize, in int range, in FixedList128Bytes<int> ignoreLayers, out int count)
        {
            GridSystem grid = PresentationSystem<DefaultPresentationGroup, GridSystem>.System;

            grid.GetRange(in buffer, in bufferSize, positions[0].index, in range, in ignoreLayers, out count);
        }

        public bool HasPath(int to, in int maxIteration = 32) => HasPath(in to, out _, maxIteration);
        public bool HasPath(in int to, out int pathCount, in int maxIteration = 32)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.HasPath(
                positions[0].index, 
                to, 
                out pathCount, 
                m_Parent.GetAttribute<GridSizeAttribute>().ObstacleLayers,
                maxIteration);
        }

        public bool HasDirection(GridPosition position, Direction direction, out GridPosition target)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.HasDirection(in position.index, in direction, out target);
        }

        public bool GetPath64(in int to, ref GridPath64 path, in int maxIteration = 32)
        {
            return PresentationSystem<DefaultPresentationGroup, GridSystem>.System.GetPath64(
                positions[0].index, 
                in to, 
                ref path, 
                m_Parent.GetAttribute<GridSizeAttribute>().ObstacleLayers, 
                in maxIteration);
        }

        public GridPosition GetGridPosition(in int index)
        {
            return new GridPosition(index, PresentationSystem<DefaultPresentationGroup, GridSystem>.System.IndexToLocation(index));
        }
    }
}
