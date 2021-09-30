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
    }
}
