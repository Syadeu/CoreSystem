using Unity.Collections;

namespace Syadeu.Presentation.Map
{
    public struct GridPath16
   {
        public static GridPath16 Create()
        {
            return new GridPath16()
            {
                m_Paths = new FixedList32Bytes<GridTile>()
            };
        }

        private FixedList32Bytes<GridTile> m_Paths;

        public FixedList32Bytes<GridTile> Paths => m_Paths;
        public int Length => m_Paths.Length;
        public GridTile this[int index]
        {
            get => m_Paths[index];
            set => m_Paths[index] = value;
        }

        public GridPath16(in FixedList32Bytes<GridTile> paths)
        {
            m_Paths = paths;
        }

        public void Clear() => m_Paths.Clear();
        public void Add(in GridTile pathTile) => m_Paths.Add(in pathTile);
        public void RemoveAt(in int index) => m_Paths.RemoveAt(index);
    }
    public struct GridPath32
    {
        public static GridPath32 Create()
        {
            return new GridPath32()
            {
                m_Paths = new FixedList64Bytes<GridTile>()
            };
        }

        private FixedList64Bytes<GridTile> m_Paths;

        public FixedList64Bytes<GridTile> Paths => m_Paths;
        public int Length => m_Paths.Length;
        public GridTile this[int index]
        {
            get => m_Paths[index];
            set => m_Paths[index] = value;
        }

        public GridPath32(in FixedList64Bytes<GridTile> paths)
        {
            m_Paths = paths;
        }

        public void Clear() => m_Paths.Clear();
        public void Add(in GridTile pathTile) => m_Paths.Add(in pathTile);
        public void RemoveAt(in int index) => m_Paths.RemoveAt(index);
    }
}
