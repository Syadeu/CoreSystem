﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Database
{
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

            if (idx == 0) return new int2(0, 0);
            else return new int2(zSize % idx, zSize / idx);
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
    public sealed class ManagedGrid
    {
        [JsonProperty] internal readonly Hash m_Hash;
        [JsonProperty] private readonly AABB m_AABB;

        [JsonProperty] private readonly int m_Length;
        [JsonProperty] private readonly float m_CellSize;

        [JsonProperty] private readonly Dictionary<int, ManagedCell> m_Cells;

        [JsonIgnore] public int2 gridSize => new int2(
            (int)math.floor(m_AABB.size.x / m_CellSize),
            (int)math.floor(m_AABB.size.z / m_CellSize));

        [JsonIgnore] public float cellSize => m_CellSize;
        [JsonIgnore] public float3 center => m_AABB.center;
        [JsonIgnore] public float3 size => m_AABB.size;
        [JsonIgnore] public ManagedCell[] cells => m_Cells.Values.ToArray();

        public ManagedGrid(int3 center, int3 size, float cellSize)
        {
            m_Hash = Hash.NewHash();
            m_AABB = new AABB(center, size);

            m_CellSize = cellSize;
            m_Cells = new Dictionary<int, ManagedCell>();

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

        public ManagedCell GetCell(int idx)
        {
            if (idx >= m_Length) throw new Exception();

            if (!m_Cells.TryGetValue(idx, out var cell))
            {
                cell = new ManagedCell(
                            m_Hash, idx,
                            GridExtensions.IndexToPosition(in m_AABB, in m_CellSize, in idx),
                            m_CellSize
                            );
                m_Cells.Add(idx, cell);
            }
            return cell;
        }
        public ManagedCell GetCell(float3 position)
        {
            if (!HasCell(position)) throw new Exception();

            int idx = GridExtensions.PositionToIndex(in m_AABB, in m_CellSize, in position);
            if (!m_Cells.TryGetValue(idx, out var cell))
            {
                cell = new ManagedCell(
                            m_Hash, idx,
                            GridExtensions.IndexToPosition(in m_AABB, in m_CellSize, in idx),
                            m_CellSize
                            );
                m_Cells.Add(idx, cell);
            }
            return cell;
        }

        #endregion

        public IReadOnlyList<ManagedCell> GetRange(in int idx, in int range)
            => GetRange(GridExtensions.IndexToLocation(in m_AABB, in m_CellSize, in idx), range);
        public IReadOnlyList<ManagedCell> GetRange(in int2 location, in int range)
        {
            int2 gridSize = this.gridSize;
            float3 cellSize = new float3(m_CellSize, m_AABB.size.y, m_CellSize);

            List<ManagedCell> targets = new List<ManagedCell>();
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
                        if (!m_Cells.TryGetValue(temp, out ManagedCell cell))
                        {
                            cell = new ManagedCell(
                            m_Hash, temp,
                            GridExtensions.IndexToPosition(in m_AABB, in m_CellSize, in temp),
                            cellSize
                            );
                        }

                        targets.Add(cell);
                    }
                    if (temp >= (gridSize.y * (location.y - yGrid + 2)) + gridSize.x - 1) break;
                }
            }

            return targets;
        }

        public byte[] ToBinary()
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            using (Newtonsoft.Json.Bson.BsonDataWriter wr = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
            {
                Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
                serializer.Serialize(wr, this);

                return ms.ToArray();
            }
        }
        public static ManagedGrid FromBinary(in byte[] data)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(data))
            using (Newtonsoft.Json.Bson.BsonDataReader rd = new Newtonsoft.Json.Bson.BsonDataReader(ms))
            {
                Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
                return serializer.Deserialize<ManagedGrid>(rd);
            }
        }
    }
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
