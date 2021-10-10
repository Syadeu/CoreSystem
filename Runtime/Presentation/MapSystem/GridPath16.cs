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
    public struct GridPath64
    {
        public static GridPath64 Create()
        {
            return new GridPath64()
            {
                m_Paths = new FixedList512Bytes<GridTile>()
            };
        }

        private FixedList512Bytes<GridTile> m_Paths;

        public FixedList512Bytes<GridTile> Paths => m_Paths;
        public int Length => m_Paths.Length;
        public GridTile this[int index]
        {
            get => m_Paths[index];
            set => m_Paths[index] = value;
        }

        public GridPath64(in FixedList512Bytes<GridTile> paths)
        {
            m_Paths = paths;
        }

        public void Clear() => m_Paths.Clear();
        public void Add(in GridTile pathTile) => m_Paths.Add(in pathTile);
        public void RemoveAt(in int index) => m_Paths.RemoveAt(index);
    }
}
