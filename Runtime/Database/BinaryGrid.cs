using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Database
{
    public partial struct BinaryGrid
    {
        private readonly Hash m_Hash;
        private readonly AABB m_AABB;

        private readonly int m_Length;
        private readonly float m_CellSize;

        public BinaryGrid(int3 center, int3 size, float cellSize)
        {
            m_Hash = Hash.NewHash();
            m_AABB = new AABB(center, size);

            m_CellSize = cellSize;

            int
                xSize = (int)math.floor(size.x / cellSize),
                zSize = (int)math.floor(size.z / cellSize);
            m_Length = xSize * zSize;
        }

        public bool HasCell(int idx) => idx < m_Length;
        public bool HasCell(int x, int y)
        {
            if (x < 0 || y < 0 ||
                    x > m_AABB.size.x || y > m_AABB.size.z) return false;

            return HasCell(LocationToIndex(m_AABB, x, y));
        }
        public bool HasCell(float3 position) => HasCell(PositionToIndex(in m_AABB, in m_CellSize, position));
        
        public AABB GetCell(float3 position)
        {
            if (!HasCell(position)) throw new Exception();
            return PositionToAABB(in m_AABB, in m_CellSize, in position);
        }

        public static byte[] GridToBinary(in BinaryGrid grid)
        {
            byte[] vs;
            unsafe
            {
                int bufferSize = Marshal.SizeOf<BinaryGrid>();
                vs = new byte[bufferSize];

                fixed (BinaryGrid* g = &grid)
                {
                    Marshal.Copy((IntPtr)g, vs, 0, bufferSize);
                }
            }
            return vs;
        }

        #region Math
        public static int2 PositionToLocation(in AABB aabb, in float cellSize, in float3 pos)
        {
            float
                half = cellSize * .5f,
                firstCenterX = aabb.min.x + half + (cellSize * 1),
                firstCenterZ = aabb.max.z - half - (cellSize * 1);

            int 
                x = math.abs(Convert.ToInt32((pos.x - firstCenterX) / cellSize)),
                y = math.abs(Convert.ToInt32((pos.z - firstCenterZ) / cellSize));

            return new int2(x, y);
        }
        public static int PositionToIndex(in AABB aabb, in float cellSize, in float3 pos)
        {
            int2 location = PositionToLocation(in aabb, in cellSize, in pos);
            return LocationToIndex(in aabb, in cellSize, in location);
        }
        public static int LocationToIndex(in AABB aabb, in float cellSize, in int2 xy) => LocationToIndex(in aabb, in cellSize, in xy.x, in xy.y);
        public static int LocationToIndex(in AABB aabb, in float cellSize, in int x, in int y)
        {
            int zSize = Mathf.FloorToInt(aabb.size.z / cellSize);
            return zSize * y + x;
        }
        public static float3 LocationToPosition(in AABB aabb, in float cellSize, in int2 xy) => LocationToPosition(in aabb, in cellSize, in xy.x, in xy.y);
        public static float3 LocationToPosition(in AABB aabb, in float cellSize, in int x, in int y)
        {
            float
                half = cellSize * .5f,
                targetX = aabb.min.x + half + (cellSize * x),
                targetY = aabb.center.y,
                targetZ = aabb.max.z - half - (cellSize * y);
            return new float3(targetX, targetY, targetZ);
        }
        public static int2 IndexToLocation(in AABB aabb, in float cellSize, in int idx)
        {
            int zSize = (int)math.floor(aabb.size.z / cellSize);
            return new int2(zSize % idx, zSize / idx);
        }
        public static float3 IndexToPosition(in AABB aabb, in float cellSize, in int idx)
        {
            int2 location = IndexToLocation(in aabb, in cellSize, in idx);
            return LocationToPosition(in aabb, in cellSize, in location);
        }

        public static AABB PositionToAABB(in AABB aabb, in float cellSize, in float3 pos)
        {
            float3 center = LocationToPosition(in aabb, in cellSize, PositionToLocation(in aabb, in cellSize, in pos));
            return new AABB(center, new float3(cellSize, aabb.size.y, cellSize));
        }
        public static AABB IndexToAABB(in AABB aabb, in float cellSize, in int cellIdx)
        {
            float3 center = IndexToPosition(in aabb, in cellSize, in cellIdx);
            return new AABB(center, new float3(cellSize, aabb.size.y, cellSize));
        }
        public static int AABBToIndex(in AABB aabb, in AABB cellAabb) => PositionToIndex(in aabb, cellAabb.size.x, cellAabb.center);
        public static int2 AABBToLocation(in AABB aabb, in AABB cellAabb) => PositionToLocation(in aabb, cellAabb.size.x, cellAabb.center);
        #endregion
    }
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct AABB
    {
        private float3 m_Center;
        private float3 m_Extents;

        public AABB(float3 center, float3 size)
        {
            m_Center = center;
            m_Extents = size * .5f;
        }
        public AABB(int3 center, int3 size)
        {
            m_Center = center;
            m_Extents = new float3(size) * .5f;
        }

        public Vector3 center { get { return m_Center; } set { m_Center = value; } }
        public Vector3 size { get { return m_Extents * 2.0F; } set { m_Extents = value * 0.5F; } }
        public Vector3 extents { get { return m_Extents; } set { m_Extents = value; } }
        public Vector3 min { get { return center - extents; } set { SetMinMax(value, max); } }
        public Vector3 max { get { return center + extents; } set { SetMinMax(min, value); } }

        public void SetMinMax(Vector3 min, Vector3 max)
        {
            extents = (max - min) * 0.5F;
            center = min + extents;
        }
    }
    internal static class BinaryGridHelper
    {
        static void test<T>() where T : unmanaged { }
        static void testest()
        {
            test<BinaryGrid>();
        }
    }
    //public partial struct BinaryCell
    //{
    //    private readonly AABB m_AABB;

    //    public AABB AABB => m_AABB;

    //    public BinaryCell(float3 center, float3 size)
    //    {
    //        m_AABB = new AABB(center, size);
    //    }
    //}
}
