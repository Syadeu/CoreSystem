using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Collections
{
    [Obsolete]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct BinaryGrid<T> : IDisposable where T : unmanaged
    {
        internal readonly Hash m_Hash;
        private readonly AABB m_AABB;

        private readonly int m_Length;
        private readonly float m_CellSize;

        public int2 gridSize => new int2(
            (int)math.floor(m_AABB.size.x / m_CellSize),
            (int)math.floor(m_AABB.size.z / m_CellSize));
        public float cellSize => m_CellSize;

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

        #region Get Set

        public bool HasCell(int idx) => idx < m_Length;
        public bool HasCell(int x, int y)
        {
            if (x < 0 || y < 0 ||
                    x > m_AABB.size.x || y > m_AABB.size.z) return false;

            return HasCell(GridExtensions.LocationToIndex(m_AABB, x, y));
        }
        public bool HasCell(float3 position) => HasCell(GridExtensions.PositionToIndex(in m_AABB, in m_CellSize, position));
        
        public AABB GetCell(float3 position)
        {
            if (!HasCell(position)) throw new Exception();
            return GridExtensions.PositionToAABB(in m_AABB, in m_CellSize, in position);
        }

        public float3 GetCellPosition(int idx) => GridExtensions.IndexToPosition(in m_AABB, in m_CellSize, in idx);
        public float3 GetCellPosition(int2 location) => GridExtensions.LocationToPosition(in m_AABB, in m_CellSize, in location);
        public float3 GetCellPosition(float3 position)
        {
            int2 idx = GridExtensions.PositionToLocation(in m_AABB, in m_CellSize, in position);
            return GridExtensions.LocationToPosition(in m_AABB, in m_CellSize, in idx);
        }

        #endregion

        public IReadOnlyList<BinaryCell<T>> GetRange(in int idx, in int range)
            => GetRange(GridExtensions.IndexToLocation(in m_AABB, in m_CellSize, in idx), range);
        public IReadOnlyList<BinaryCell<T>> GetRange(in int2 location, in int range)
        {
            int2 gridSize = this.gridSize;
            float3 cellSize = new float3(m_CellSize, m_AABB.size.y, m_CellSize);

            List<BinaryCell<T>> targets = new List<BinaryCell<T>>();
            // 왼쪽 아래 부터 탐색 시작
            int startIdx = (gridSize.y * (location.y + range)) + location.x - range;

            int height = ((range * 2) + 1);
            for (int yGrid = 0; yGrid < height; yGrid++)
            {
                for (int xGrid = 0; xGrid < height; xGrid++)
                {
                    int temp = startIdx - (yGrid * gridSize.y) + xGrid;
                    if (HasCell(temp))
                    {
                        BinaryCell<T> cell = new BinaryCell<T>(
                            m_Hash, temp,
                            GridExtensions.IndexToPosition(in m_AABB, in m_CellSize, in temp),
                            cellSize
                            );

                        targets.Add(cell);
                    }
                    if (temp >= (gridSize.y * (location.y - yGrid + 2)) + gridSize.x - 1) break;
                }
            }

            return targets;
        }

        public void Dispose()
        {
            this.ClearData();
        }

        public static BinaryGrid<T> FromBinary(in byte[] vs) => GridExtensions.FromBinary<T>(vs);
    }
    public static class GridExtensions
    {
        private const string DEFAULT_MATERIAL = "Sprites-Default.mat";
        private static Material s_Material;
        public static Material DefaultMaterial
        {
            get
            {
                if (s_Material == null)
                {
#if UNITY_EDITOR
                    s_Material = AssetDatabase.GetBuiltinExtraResource<Material>(DEFAULT_MATERIAL);
                    if (s_Material == null)
#endif
                        s_Material = Resources.GetBuiltinResource<Material>(DEFAULT_MATERIAL);
                }
                return s_Material;
            }
        }
        private static readonly Dictionary<Hash, object> m_GridData = new Dictionary<Hash, object>();

        private struct Payload<T> where T : unmanaged
        {
            public int m_CellIdx;
            public T m_Value;
        }

        #region Data Works
        internal static BinaryGrid<T> FromBinary<T>(in byte[] vs) where T : unmanaged
        {
            int gridSize = Marshal.SizeOf<BinaryGrid<T>>();
            
            BinaryGrid<T> grid = new BinaryGrid<T>();
            unsafe
            {
                BinaryGrid<T>* g = &grid;
                IntPtr gPtr = (IntPtr)g;
                Marshal.Copy(vs, 0, gPtr, gridSize);
            }
            if (vs.Length.Equals(gridSize)) return grid;

            int payloadSize = Marshal.SizeOf<Payload<T>>();
            int startIdx = vs.Length - gridSize;
            int payloadCount = startIdx / payloadSize;
            
            for (int i = 0, j = startIdx; i < payloadCount; i++, j += payloadSize)
            {
                Payload<T> payload = new Payload<T>();
                unsafe
                {
                    Payload<T>* p = &payload;
                    IntPtr ptr = (IntPtr)p;
                    Marshal.Copy(vs, j, ptr, payloadSize);
                }

                grid.SetData(payload.m_CellIdx, payload.m_Value);
            }

            return grid;
        }
        public static byte[] ToBinary<T>(this BinaryGrid<T> grid) where T : unmanaged
        {
            byte[] vs;
            int bufferSize = 0;
            int gridSize = Marshal.SizeOf<BinaryGrid<T>>();
            bufferSize += gridSize;

            Payload<T>[] payloads = null;
            int payloadSize = 0;
            if (m_GridData.ContainsKey(grid.m_Hash))
            {
                NativeHashMap<int, T> dataSetDic = GetDataSet<T>(grid.m_Hash);
                if (dataSetDic.Count() > 0)
                {
                    NativeKeyValueArrays<int, T> dataSet = dataSetDic.GetKeyValueArrays(Allocator.Temp);
                    payloads = new Payload<T>[dataSet.Length];

                    payloadSize = Marshal.SizeOf<Payload<T>>();
                    bufferSize += payloadSize * dataSet.Length;

                    for (int i = 0; i < dataSet.Length; i++)
                    {
                        payloads[i] = new Payload<T>
                        {
                            m_CellIdx = dataSet.Keys[i],
                            m_Value = dataSet.Values[i]
                        };
                    }
                    dataSet.Dispose();
                }
            }
            vs = new byte[bufferSize];

            unsafe
            {
                BinaryGrid<T>* g = &grid;
                Marshal.Copy((IntPtr)g, vs, 0, gridSize);

                if (payloads != null)
                {
                    for (int i = 0, j = gridSize; j < bufferSize; i++, j += payloadSize)
                    {
                        fixed (Payload<T>* p = &payloads[i])
                        {
                            Marshal.Copy((IntPtr)p, vs, j, payloadSize);
                        }
                    }
                }
            }
            return vs;
        }
        public static void SetData<T>(this BinaryGrid<T> grid, int idx, T data) where T : unmanaged
        {
            NativeHashMap<int, T> dataList = GetDataSet<T>(grid.m_Hash);
            if (dataList.ContainsKey(idx))
            {
                dataList[idx] = data;
            }
            else dataList.Add(idx, data);
        }
        public static void SetData<T>(this BinaryCell<T> cell, T data) where T : unmanaged
        {
            NativeHashMap<int, T> dataList = GetDataSet<T>(cell.m_Parent);
            if (dataList.ContainsKey(cell.m_Idx))
            {
                dataList[cell.m_Idx] = data;
            }
            else dataList.Add(cell.m_Idx, data);
        }
        public static T GetData<T>(this BinaryGrid<T> grid, int idx) where T : unmanaged
        {
            NativeHashMap<int, T> dataList = GetDataSet<T>(grid.m_Hash);
            if (dataList.ContainsKey(idx))
            {
                return dataList[idx];
            }
            else throw new Exception();
        }
        public static T GetData<T>(this BinaryCell<T> cell) where T : unmanaged
        {
            NativeHashMap<int, T> dataList = GetDataSet<T>(cell.m_Parent);
            if (dataList.ContainsKey(cell.m_Idx))
            {
                return dataList[cell.m_Idx];
            }
            else throw new Exception();
        }
        public static void RemoveData<T>(this BinaryGrid<T> grid, int idx) where T : unmanaged
        {
            NativeHashMap<int, T> dataList = GetDataSet<T>(grid.m_Hash);
            if (dataList.ContainsKey(idx))
            {
                dataList.Remove(idx);
            }
        }
        public static void RemoveData<T>(this BinaryCell<T> cell) where T : unmanaged
        {
            NativeHashMap<int, T> dataList = GetDataSet<T>(cell.m_Parent);
            if (dataList.ContainsKey(cell.m_Idx))
            {
                dataList.Remove(cell.m_Idx);
            }
        }
        public static void ClearData<T>(this BinaryGrid<T> grid) where T : unmanaged
        {
            RemoveDataSet<T>(grid.m_Hash);
        }

        private static NativeHashMap<int, T> GetDataSet<T>(Hash hash) where T : unmanaged
        {
            NativeHashMap<int, T> hashMap;
            if (!m_GridData.TryGetValue(hash, out object data))
            {
                hashMap = new NativeHashMap<int, T>(128, Allocator.Persistent);
                m_GridData.Add(hash, hashMap);
            }
            else hashMap = (NativeHashMap<int, T>)data;

            return hashMap;
        }
        private static void RemoveDataSet<T>(Hash hash) where T : unmanaged
        {
            if (m_GridData.ContainsKey(hash))
            {
                m_GridData.Remove(hash);
            }
        }
        #endregion

        #region Math
        public static int2 PositionToLocation(in AABB aabb, in float cellSize, in float3 pos)
        {
            float
                half = cellSize * .5f,
                firstCenterX = aabb.min.x + half/* + (cellSize * 1)*/,
                firstCenterZ = aabb.max.z - half /*- (cellSize * 1)*/;

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
            int zSize = (int)math.floor(aabb.size.z / cellSize);
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
            if (idx == 0) return new int2(0, 0);

            int zSize = (int)math.floor(aabb.size.z / cellSize);

            int y = idx / zSize;
            int x = idx - (zSize * y);

            return new int2(x, y);
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

        #region GL

        public static void DrawGL<T>(this BinaryGrid<T> grid) where T : unmanaged
        {
            int2 gridSize = grid.gridSize;

            Vector3 minPos = grid.GetCellPosition(0);
            minPos.x -= grid.cellSize * .5f;
            minPos.z += grid.cellSize * .5f;

            Vector3 maxPos = grid.GetCellPosition(gridSize);
            maxPos.x -= grid.cellSize * .5f;
            maxPos.z += grid.cellSize * .5f;

            GL.PushMatrix();
            DefaultMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            for (int y = 0; y < gridSize.y + 1; y++)
            {
                for (int x = 0; x < gridSize.x + 1; x++)
                {
                    Vector3
                        p1 = new Vector3(
                            minPos.x,
                            minPos.y,
                            minPos.z - (grid.cellSize * y)),
                        p2 = new Vector3(
                            maxPos.x,
                            minPos.y,
                            minPos.z - (grid.cellSize * y)),
                        p3 = new Vector3(
                            minPos.x + (grid.cellSize * x),
                            minPos.y,
                            minPos.z),
                        p4 = new Vector3(
                            minPos.x + (grid.cellSize * x),
                            minPos.y,
                            maxPos.z)
                        ;

                    GL.Vertex(p1); GL.Vertex(p2);
                    GL.Vertex(p3); GL.Vertex(p4);
                }
            }
            GL.End();
            GL.PopMatrix();
        }
        public static void DrawGL(this BinaryGrid grid, float thickness, Camera cam = null)
        {
            int2 gridSize = grid.gridSize;

            Vector3 minPos = grid.IndexToPosition(0);
            minPos.x -= grid.cellSize * .5f;
            minPos.z += grid.cellSize * .5f;

            Vector3 maxPos = grid.LocationToPosition(gridSize);
            maxPos.x -= grid.cellSize * .5f;
            maxPos.z += grid.cellSize * .5f;

            GL.PushMatrix();
            if (cam != null)
            {
                float3x3 rotmat = new float3x3(quaternion.identity);
                float4x4 mat = new float4x4(rotmat, float3.zero);
                GL.MultMatrix(mat);
                GL.LoadProjectionMatrix(cam.projectionMatrix);
            }
            DefaultMaterial.SetPass(0);
            GL.Begin(GL.QUADS);

            var xTemp = new Vector3(thickness * .5f, 0, 0);
            var yTemp = new Vector3(0, 0, thickness * .5f);

            for (int y = 0; y < gridSize.y + 1; y++)
            {
                for (int x = 0; x < gridSize.x + 1; x++)
                {
                    Vector3
                        p1 = new Vector3(
                            minPos.x,
                            minPos.y + .05f,
                            minPos.z - (grid.cellSize * y)),
                        p2 = new Vector3(
                            maxPos.x,
                            minPos.y + .05f,
                            minPos.z - (grid.cellSize * y)),
                        p3 = new Vector3(
                            minPos.x + (grid.cellSize * x),
                            minPos.y + .05f,
                            minPos.z),
                        p4 = new Vector3(
                            minPos.x + (grid.cellSize * x),
                            minPos.y + .05f,
                            maxPos.z)
                        ;

                    GL.Vertex(p1 - yTemp); GL.Vertex(p2 - yTemp);
                    GL.Vertex(p2 + yTemp); GL.Vertex(p1 + yTemp);

                    GL.Vertex(p3 - xTemp); GL.Vertex(p4 - xTemp);
                    GL.Vertex(p4 + xTemp); GL.Vertex(p3 + xTemp);
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        #endregion
    }
    [Obsolete]
    public partial struct BinaryCell<T> : IValidation where T : unmanaged
    {
        internal readonly Hash m_Parent;
        internal readonly int m_Idx;
        private readonly AABB m_AABB;

        public AABB AABB => m_AABB;

        internal BinaryCell(Hash grid, int idx, float3 center, float3 size)
        {
            m_Parent = grid;
            m_Idx = idx;
            m_AABB = new AABB(center, size);
        }

        public bool IsValid() => !m_Parent.Equals(Hash.Empty);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BinaryGrid : IValidation, IEquatable<BinaryGrid>
    {
        private readonly AABB m_AABB;

        private readonly int m_Length;
        private readonly float m_CellSize;

        public int length => m_Length;
        public int2 gridSize => new int2(
            (int)math.floor(m_AABB.size.x / m_CellSize),
            (int)math.floor(m_AABB.size.z / m_CellSize));

        public float cellSize => m_CellSize;
        public float3 center => m_AABB.center;
        public float3 size => m_AABB.size;
        public AABB bounds => m_AABB;

        public BinaryGrid(int3 center, int3 size, float cellSize)
        {
            m_AABB = new AABB(center, size);

            m_CellSize = cellSize;

            int
                xSize = (int)math.floor(size.x / cellSize),
                zSize = (int)math.floor(size.z / cellSize);
            m_Length = xSize * zSize;
        }

        #region Get Set

        public bool HasCell(int idx) => idx < m_Length;
        public bool HasCell(int x, int y)
        {
            if (x < 0 || y < 0 ||
                    x > m_AABB.size.x || y > m_AABB.size.z) return false;

            return HasCell(GridExtensions.LocationToIndex(m_AABB, x, y));
        }
        public bool HasCell(in float3 position) => m_AABB.Contains(position) && HasCell(GridExtensions.PositionToIndex(in m_AABB, in m_CellSize, in position));

        //public ManagedCell GetCell(int idx)
        //{
        //    if (idx >= m_Length) throw new Exception();

        //    if (!m_Cells.TryGetValue(idx, out var cell))
        //    {
        //        cell = new ManagedCell(
        //                    m_Hash, idx,
        //                    GridExtensions.IndexToPosition(in m_AABB, in m_CellSize, in idx),
        //                    m_CellSize
        //                    );
        //        m_Cells.Add(idx, cell);
        //    }
        //    return cell;
        //}
        //public ManagedCell GetCell(float3 position)
        //{
        //    if (!HasCell(position)) throw new Exception();

        //    int idx = GridExtensions.PositionToIndex(in m_AABB, in m_CellSize, in position);
        //    if (!m_Cells.TryGetValue(idx, out var cell))
        //    {
        //        cell = new ManagedCell(
        //                    m_Hash, idx,
        //                    GridExtensions.IndexToPosition(in m_AABB, in m_CellSize, in idx),
        //                    m_CellSize
        //                    );
        //        m_Cells.Add(idx, cell);
        //    }
        //    return cell;
        //}

        public int PositionToIndex(float3 position) => GridExtensions.PositionToIndex(in m_AABB, in m_CellSize, in position);
        public int2 PositionToLocation(float3 position) => GridExtensions.PositionToLocation(in m_AABB, in m_CellSize, in position);

        public float3 IndexToPosition(in int idx) => GridExtensions.IndexToPosition(in m_AABB, in m_CellSize, in idx);
        public int2 IndexToLocation(in int idx) => GridExtensions.IndexToLocation(in m_AABB, in m_CellSize, in idx);

        public float3 LocationToPosition(int2 location) => GridExtensions.LocationToPosition(in m_AABB, in m_CellSize, in location);
        public int LocationToIndex(in int2 location) => GridExtensions.LocationToIndex(in m_AABB, in m_CellSize, in location);

        public float3 PositionToPosition(float3 position)
        {
            int2 idx = GridExtensions.PositionToLocation(in m_AABB, in m_CellSize, in position);
            return GridExtensions.LocationToPosition(in m_AABB, in m_CellSize, in idx);
        }

        #endregion

        #region Get Ranges

        public FixedList32Bytes<int> GetRange8(in int idx, in int range)
        {
            int2 gridSize = this.gridSize;
            FixedList32Bytes<int> targets = new FixedList32Bytes<int>();

            int count = 0;

            int startIdx = idx - range + (gridSize.y * range);
            int height = ((range * 2) + 1);
            for (int yGrid = 0; yGrid < height; yGrid++)
            {
                for (int xGrid = 0; xGrid < height; xGrid++)
                {
                    int temp = startIdx - (yGrid * gridSize.y) + xGrid;

                    if (HasCell(temp))
                    {
                        targets.Add(temp);
                        count += 1;
                    }
                    //if (temp >= temp - (temp % gridSize.x) + gridSize.x - 1) break;
                }
            }
            return targets;
        }
        public FixedList64Bytes<int> GetRange16(in int idx, in int range)
        {
            int2 gridSize = this.gridSize;
            FixedList64Bytes<int> targets = new FixedList64Bytes<int>();

            int count = 0;

            int startIdx = idx - range + (gridSize.y * range);
            int height = ((range * 2) + 1);
            for (int yGrid = 0; yGrid < height; yGrid++)
            {
                for (int xGrid = 0; xGrid < height; xGrid++)
                {
                    int temp = startIdx - (yGrid * gridSize.y) + xGrid;

                    if (HasCell(temp))
                    {
                        targets.Add(temp);
                        count += 1;
                    }
                    //if (temp >= temp - (temp % gridSize.x) + gridSize.x - 1) break;
                }
            }
            return targets;
        }
        public FixedList128Bytes<int> GetRange32(in int idx, in int range)
        {
            int2 gridSize = this.gridSize;
            FixedList128Bytes<int> targets = new FixedList128Bytes<int>();

            int count = 0;

            int startIdx = idx - range + (gridSize.y * range);
            int height = ((range * 2) + 1);
            for (int yGrid = 0; yGrid < height; yGrid++)
            {
                for (int xGrid = 0; xGrid < height; xGrid++)
                {
                    int temp = startIdx - (yGrid * gridSize.y) + xGrid;

                    if (HasCell(temp))
                    {
                        targets.Add(temp);
                        count += 1;
                    }
                    //if (temp >= temp - (temp % gridSize.x) + gridSize.x - 1) break;
                }
            }
            return targets;
        }
        public FixedList4096Bytes<int> GetRange1024(in int idx, in int range)
        {
            int2 gridSize = this.gridSize;
            FixedList4096Bytes<int> targets = new FixedList4096Bytes<int>();

            int count = 0;

            int startIdx = idx - range + (gridSize.y * range);
            int height = ((range * 2) + 1);
            for (int yGrid = 0; yGrid < height; yGrid++)
            {
                for (int xGrid = 0; xGrid < height; xGrid++)
                {
                    int temp = startIdx - (yGrid * gridSize.y) + xGrid;

                    if (HasCell(temp))
                    {
                        targets.Add(temp);
                        count += 1;
                    }
                    //if (temp >= temp - (temp % gridSize.x) + gridSize.x - 1) break;
                }
            }
            return targets;
        }

        public void GetRange(ref NativeList<int> targets, in int idx, in int range)
        {
            targets.Clear();
            int2 gridSize = this.gridSize;

            int count = 0;

            int startIdx = idx - range + (gridSize.y * range);
            int height = ((range * 2) + 1);
            for (int yGrid = 0; yGrid < height; yGrid++)
            {
                for (int xGrid = 0; xGrid < height; xGrid++)
                {
                    int temp = startIdx - (yGrid * gridSize.y) + xGrid;

                    if (HasCell(temp))
                    {
                        //$"add {temp}".ToLog();
                        targets.Add(temp);
                        count += 1;
                    }
                    //if (temp >= temp - (temp % gridSize.x) + gridSize.x - 1) break;
                }
            }
        }
        unsafe public void GetRange(int* buffer, in int idx, in int range, in int maxRange, out int count)
        {
            //targets.Clear();
            int2 gridSize = this.gridSize;

            count = 0;

            int startIdx = idx - range + (gridSize.y * range);
            int height = ((range * 2) + 1);
            for (int yGrid = 0; yGrid < height; yGrid++)
            {
                for (int xGrid = 0; xGrid < height; xGrid++)
                {
                    int temp = startIdx - (yGrid * gridSize.y) + xGrid;

                    if (HasCell(temp))
                    {
                        //$"add {temp}".ToLog();
                        buffer[count] = (temp);
                        count += 1;
                    }
                    //if (temp >= temp - (temp % gridSize.x) + gridSize.x - 1) break;

                    if (count >= maxRange) return;
                }
            }
        }

        [Obsolete]
        public int[] GetRange(in int idx, in int range)
        {
            int2 gridSize = this.gridSize;
            List<int> targets = new List<int>();

            int startIdx = idx - range + (gridSize.y * range);
            int height = ((range * 2) + 1);
            for (int yGrid = 0; yGrid < height; yGrid++)
            {
                for (int xGrid = 0; xGrid < height; xGrid++)
                {
                    int temp = startIdx - (yGrid * gridSize.y) + xGrid;

                    if (HasCell(temp))
                    {
                        //$"add {temp}".ToLog();
                        targets.Add(temp);
                    }
                    //if (temp >= temp - (temp % gridSize.x) + gridSize.x - 1) break;
                }
            }

            return targets.ToArray();
        }
        [Obsolete]
        public int[] GetRange(in int2 location, in int range) => GetRange(GridExtensions.LocationToIndex(in m_AABB, in m_CellSize, in location), in range);

        #endregion

        public int2 GetDirection(in int from, in Direction direction)
            => GetDirection(IndexToLocation(in from), in direction);
        public int2 GetDirection(in int2 from, in Direction direction)
        {
            int2 location = from;
            if ((direction & Direction.Up) == Direction.Up)
            {
                location.y += 1;
            }
            if ((direction & Direction.Down) == Direction.Down)
            {
                location.y -= 1;
            }
            if ((direction & Direction.Left) == Direction.Left)
            {
                location.x -= 1;
            }
            if ((direction & Direction.Right) == Direction.Right)
            {
                location.x += 1;
            }
            return location;
        }

        public bool IsValid() => m_Length != 0;
        public bool Equals(BinaryGrid other)
        {
            return m_Length == other.length && m_CellSize == other.cellSize && m_AABB.Equals(other.m_AABB);
        }
    }
    [Obsolete]
    public sealed class ManagedCell
    {
        [JsonProperty] internal readonly Hash m_Parent;
        [JsonProperty] internal readonly int m_Idx;
        [JsonProperty] private readonly AABB m_AABB;
        [JsonProperty] private object m_Value;

        [JsonIgnore] public AABB AABB => m_AABB;

        [JsonConstructor] public ManagedCell() { }
        internal ManagedCell(Hash grid, int idx, float3 center, float3 size)
        {
            m_Parent = grid;
            m_Idx = idx;
            m_AABB = new AABB(center, size);
        }

        public object GetValue() => m_Value;
        public void SetValue(object value) => m_Value = value;
    }
}
