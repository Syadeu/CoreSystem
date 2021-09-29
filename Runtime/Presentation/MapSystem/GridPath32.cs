using Unity.Collections;

namespace Syadeu.Presentation.Map
{
    public struct GridPath32
    {
        public static GridPath32 Create()
        {
            return new GridPath32()
            {
                m_Paths = new FixedList32Bytes<GridPathTile>()
            };
        }

        private FixedList32Bytes<GridPathTile> m_Paths;

        public FixedList32Bytes<GridPathTile> Paths => m_Paths;
        public int Length => m_Paths.Length;
        public GridPathTile this[int index]
        {
            get => m_Paths[index];
            set => m_Paths[index] = value;
        }

        public GridPath32(in FixedList32Bytes<GridPathTile> paths)
        {
            m_Paths = paths;
        }

        public void Clear() => m_Paths.Clear();
        public void Add(in GridPathTile pathTile) => m_Paths.Add(in pathTile);
        public void RemoveAt(in int index) => m_Paths.RemoveAt(index);
    }
    public struct GridPath64
    {
        public static GridPath64 Create()
        {
            return new GridPath64()
            {
                m_Paths = new FixedList64Bytes<GridPathTile>()
            };
        }

        private FixedList64Bytes<GridPathTile> m_Paths;

        public FixedList64Bytes<GridPathTile> Paths => m_Paths;
        public int Length => m_Paths.Length;
        public GridPathTile this[int index]
        {
            get => m_Paths[index];
            set => m_Paths[index] = value;
        }

        public GridPath64(in FixedList64Bytes<GridPathTile> paths)
        {
            m_Paths = paths;
        }

        public void Clear() => m_Paths.Clear();
        public void Add(in GridPathTile pathTile) => m_Paths.Add(in pathTile);
        public void RemoveAt(in int index) => m_Paths.RemoveAt(index);
    }
}
