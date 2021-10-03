using Unity.Mathematics;

namespace Syadeu.Presentation.Map
{
    public struct GridPosition4 : IGridPosition
    {
        public static readonly GridPosition4 Empty = new GridPosition4(-1, -1);

        public int4 index;
        public int2x4 location;

        public int Length
        {
            get
            {
                int length = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (this[i].IsEmpty()) break;

                    length += 1;
                }
                return length;
            }
        }

        int2 IGridPosition.this[int i]
        {
            get => location[i];
        }
        public GridPosition this[int i]
        {
            get => new GridPosition(index[i], location[i]);
            set
            {
                index[i] = value.index;
                location[i] = value.location;
            }
        }

        public GridPosition4(int4 index, int2x4 location)
        {
            this.index = index;
            this.location = location;
        }
        public GridPosition4(GridPosition a0, GridPosition a1, GridPosition a2, GridPosition a3)
        {
            this = default(GridPosition4);

            index[0] = a0.index;
            location[0] = a0.location;
            
            index[1] = a1.index;
            location[1] = a1.location;

            index[2] = a2.index;
            location[2] = a2.location;

            index[3] = a3.index;
            location[3] = a3.location;
        }

        public void Update(int4 index, int2x4 location)
        {
            this.index = index;
            this.location = location;
        }
        public void UpdateAt(int i, int index, int2 location)
        {
            this.index[i] = index;
            this.location[i] = location;
        }
        public void UpdateAt(int i, GridPosition position)
        {
            this.index[i] = position.index;
            this.location[i] = position.location;
        }
        public void RemoveAt(int i)
        {
            this.index[i] = -1;
            this.location[i] = -1;
        }

        public bool IsValid() => !Equals(Empty);
        public bool Equals(IGridPosition other)
        {
            if (!Length.Equals(other.Length)) return false;

            for (int i = 0; i < Length; i++)
            {
                if (!this[i].Equals(other)) return false;
            }

            return true;
        }

        public bool Contains(GridPosition pos)
        {
            for (int i = 0; i < 4; i++)
            {
                if (index[i].Equals(pos.index) && location[i].Equals(pos.location))
                {
                    return true;
                }
            }
            return false;
        }
        internal bool Contains(GridPathTile pos)
        {
            for (int i = 0; i < 4; i++)
            {
                if (index[i].Equals(pos.position.index) && location[i].Equals(pos.position.location))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
