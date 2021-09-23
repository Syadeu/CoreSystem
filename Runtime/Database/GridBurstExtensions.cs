using Unity.Mathematics;
using Unity.Burst;
using AOT;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
#endif

namespace Syadeu.Database
{
    [BurstCompile]
    public static class GridBurstExtensions
    {
        public delegate int LocationInt2ToIndex(in AABB aabb, in float cellSize, in int2 xy);
        public delegate int LocationIntToIndex(in AABB aabb, in float cellSize, in int x, in int y);

        public delegate void IndexIntToLocation(in AABB aabb, in float cellSize, in int idx, ref int2 output);
        public delegate void PositionFloat3ToLocation(in AABB aabb, in float cellSize, in float3 pos, ref int2 output);

        public static readonly Functions f_Functions = new Functions(
            p_LocationIntToIndex, p_LocationInt2ToIndex,
            p_IndexIntToLocation, p_PositionFloat3ToLocation);

        /*              */

        public static readonly FunctionPointer<LocationIntToIndex> p_LocationIntToIndex = BurstCompiler.CompileFunctionPointer<LocationIntToIndex>(f_LocationToIndex);
        public static readonly FunctionPointer<LocationInt2ToIndex> p_LocationInt2ToIndex = BurstCompiler.CompileFunctionPointer<LocationInt2ToIndex>(f_LocationToIndex);

        /*              */

        public static readonly FunctionPointer<IndexIntToLocation> p_IndexIntToLocation = BurstCompiler.CompileFunctionPointer<IndexIntToLocation>(f_IndexToLocation);
        public static readonly FunctionPointer<PositionFloat3ToLocation> p_PositionFloat3ToLocation = BurstCompiler.CompileFunctionPointer<PositionFloat3ToLocation>(f_PositionToLocation);

        /*              */

        public readonly struct Functions
        {
            public readonly FunctionPointer<LocationIntToIndex> p_LocationIntToIndex;
            public readonly FunctionPointer<LocationInt2ToIndex> p_LocationInt2ToIndex;

            public readonly FunctionPointer<IndexIntToLocation> p_IndexIntToLocation;
            public readonly FunctionPointer<PositionFloat3ToLocation> p_PositionFloat3ToLocation;

            public Functions(
                FunctionPointer<LocationIntToIndex> p0,
                FunctionPointer<LocationInt2ToIndex> p1,

                FunctionPointer<IndexIntToLocation> a0,
                FunctionPointer<PositionFloat3ToLocation> a1
                )
            {
                p_LocationIntToIndex = p0;
                p_LocationInt2ToIndex = p1;

                p_IndexIntToLocation = a0;
                p_PositionFloat3ToLocation = a1;
            }
        }

        #region Get Index
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [MonoPInvokeCallback(typeof(LocationInt2ToIndex))]
        private static int f_LocationToIndex(in AABB aabb, in float cellSize, in int2 xy)
        {
            int zSize = (int)math.floor(aabb.size.z / cellSize);
            return zSize * xy.y + xy.x;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [MonoPInvokeCallback(typeof(LocationIntToIndex))]
        private static int f_LocationToIndex(in AABB aabb, in float cellSize, in int x, in int y)
        {
            int zSize = (int)math.floor(aabb.size.z / cellSize);
            return zSize * y + x;
        }

        #endregion

        #region Get Location

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [MonoPInvokeCallback(typeof(IndexIntToLocation))]
        private static void f_IndexToLocation(in AABB aabb, in float cellSize, in int idx, ref int2 output)
        {
            //if (idx == 0)
            //{
            //    output = int2.zero;
            //    return;
            //}

            if (Unity.Burst.CompilerServices.Hint.Unlikely(idx == 0))
            {
                output = int2.zero;
            }
            else
            {
                int zSize = (int)math.floor(aabb.size.z / cellSize);

                int y = idx / zSize;
                int x = idx - (zSize * y);

                output = new int2(x, y);
            }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [MonoPInvokeCallback(typeof(PositionFloat3ToLocation))]
        private static void f_PositionToLocation(in AABB aabb, in float cellSize, in float3 pos, ref int2 output)
        {
            float
                half = cellSize * .5f,
                firstCenterX = aabb.min.x + half/* + (cellSize * 1)*/,
                firstCenterZ = aabb.max.z - half /*- (cellSize * 1)*/;

            int
                x = math.abs((int)((pos.x - firstCenterX) / cellSize)),
                y = math.abs((int)((pos.z - firstCenterZ) / cellSize));

            output = new int2(x, y);
        }


        #endregion


    }
}
