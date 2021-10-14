using Unity.Mathematics;
using Unity.Burst;
using AOT;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
#endif

namespace Syadeu.Collections
{
    [BurstCompile(CompileSynchronously = true)]
    public static class GridBurstExtensions
    {
        public delegate int LocationInt2ToIndex(in AABB aabb, in float cellSize, in int2 xy);
        public delegate int LocationIntToIndex(in AABB aabb, in float cellSize, in int x, in int y);

        public delegate void IndexIntToLocation(in AABB aabb, in float cellSize, in int idx, ref int2 output);
        public delegate void PositionFloat3ToLocation(in AABB aabb, in float cellSize, in float3 pos, ref int2 output);

        ///*              */

        //public static readonly FunctionPointer<LocationIntToIndex> p_LocationIntToIndex = BurstCompiler.CompileFunctionPointer<LocationIntToIndex>(f_LocationToIndex);
        //public static readonly FunctionPointer<LocationInt2ToIndex> p_LocationInt2ToIndex = BurstCompiler.CompileFunctionPointer<LocationInt2ToIndex>(f_LocationToIndex);

        ///*              */

        //public static readonly FunctionPointer<IndexIntToLocation> p_IndexIntToLocation = BurstCompiler.CompileFunctionPointer<IndexIntToLocation>(f_IndexToLocation);
        //public static readonly FunctionPointer<PositionFloat3ToLocation> p_PositionFloat3ToLocation = BurstCompiler.CompileFunctionPointer<PositionFloat3ToLocation>(f_PositionToLocation);

        ///*              */

        //public static int LocationToIndex(in AABB aabb, in float cellSize, in int2 xy)
        //    => p_LocationInt2ToIndex.Invoke(in aabb, in cellSize, in xy);
        //public static int LocationToIndex(in AABB aabb, in float cellSize, in int x, in int y)
        //    => p_LocationIntToIndex.Invoke(in aabb, in cellSize, in x, in y);

        //public static int2 IndexToLocation(in AABB aabb, in float cellSize, in int idx)
        //{
        //    int2 output = new int2();
        //    p_IndexIntToLocation.Invoke(in aabb, in cellSize, in idx, ref output);
        //    return output;
        //}
        //public static int2 PositionToLocation(in AABB aabb, in float cellSize, in float3 pos)
        //{
        //    int2 output = new int2();
        //    p_PositionFloat3ToLocation.Invoke(in aabb, in cellSize, in pos, ref output);
        //    return output;
        //}

        #region Get Index
        [BurstCompile]
        [MonoPInvokeCallback(typeof(LocationInt2ToIndex))]
        public static int f_LocationToIndex(in AABB aabb, in float cellSize, in int2 xy)
        {
            int zSize = (int)math.floor(aabb.size.z / cellSize);
            return zSize * xy.y + xy.x;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(LocationIntToIndex))]
        public static int f_LocationToIndex(in AABB aabb, in float cellSize, in int x, in int y)
        {
            int zSize = (int)math.floor(aabb.size.z / cellSize);
            return zSize * y + x;
        }

        #endregion

        #region Get Location

        [BurstCompile]
        [MonoPInvokeCallback(typeof(IndexIntToLocation))]
        public static void f_IndexToLocation(in AABB aabb, in float cellSize, in int idx, ref int2 output)
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
        [MonoPInvokeCallback(typeof(PositionFloat3ToLocation))]
        public static void f_PositionToLocation(in AABB aabb, in float cellSize, in float3 pos, ref int2 output)
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
