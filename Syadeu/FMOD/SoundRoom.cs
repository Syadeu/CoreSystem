using UnityEngine;

namespace Syadeu.FMOD
{
    [System.Serializable]
    public struct SoundRoom
    {
        public readonly static SoundRoom Null = new SoundRoom(-1);

        [SerializeField] private int m_Index;
        [SerializeField] private int m_BackgroundType;
        [SerializeField] private Bounds m_Bounds;
        [SerializeField] private float m_Direct;
        [SerializeField] private Vector3 m_Position;

        public Vector3[] m_Vertices;

        public int Index { get { return m_Index; } }
        public int BackgroundType { get { return m_BackgroundType; } }
        public Bounds Bounds { get { return m_Bounds; } }
        public float Direct { get { return m_Direct; } }
        public Vector3 Position { get { return m_Position; } }

        private bool m_Initialized;

        public SoundRoom(int type, float direct, Transform transform)
        {
            m_Index = -1;
            m_BackgroundType = type;

            m_Bounds = new Bounds
            {
                center = transform.position,
                size = Vector3.Scale(transform.lossyScale, transform.localScale)
            };
            m_Direct = Mathf.Clamp(direct, 0, 1);
            m_Position = transform.position;

            m_Vertices = new Vector3[6];
            Vector3 center = m_Bounds.center;
            // bottom
            m_Vertices[0] = new Vector3(center.x, m_Bounds.min.y, center.z);
            // left
            m_Vertices[1] = new Vector3(m_Bounds.min.x, center.y, center.z);
            // right
            m_Vertices[2] = new Vector3(m_Bounds.max.x, center.y, center.z);
            //forward
            m_Vertices[3] = new Vector3(center.x, center.y, m_Bounds.max.z);
            // backward
            m_Vertices[4] = new Vector3(center.x, center.y, m_Bounds.min.z);
            // upward
            m_Vertices[5] = new Vector3(center.x, m_Bounds.max.y, center.z);

            m_Initialized = false;
        }
        public SoundRoom(int type, float direct, Vector3 center, Vector3 size, Vector3[] vertices)
        {
            m_Index = -1;
            m_BackgroundType = type;

            m_Bounds = new Bounds
            {
                center = center,
                size = size
            };
            m_Direct = Mathf.Clamp(direct, 0, 1);
            m_Position = center;

            m_Vertices = vertices;

            m_Initialized = false;
        }
        public SoundRoom(int index)
        {
            if (index < 0)
            {
                m_Index = -1;
                m_BackgroundType = 0;
                m_Bounds = default;
                m_Direct = -1;
                m_Position = default;
            }
            else
            {
                if (FMODSystem.Instance.SoundRooms.TryGetValue(index, out SoundRoom room))
                {
                    this = room;
                }
                else
                {
                    m_Index = -1;
                    m_BackgroundType = 0;
                    m_Bounds = default;
                    m_Direct = -1;
                    m_Position = default;
                }
            }

            m_Vertices = new Vector3[6];

            m_Initialized = false;
        }
        public SoundRoom(int type, Bounds bounds, float direct, Vector3 center, Vector3[] vertices)
        {
            m_Index = -1;
            m_BackgroundType = type;
            m_Bounds = bounds;
            m_Direct = Mathf.Clamp(direct, 0, 1);
            m_Position = center;

            m_Vertices = vertices;

            m_Initialized = false;
        }

        public SoundRoom Initialize(int index)
        {
            m_Index = index;
            m_Initialized = true;
            return this;
        }

        public bool IsValid()
        {
            if (Index < 0 || this == Null) return false;
            return m_Initialized;
        }
        public bool Contains(Vector3 target)
        {
            if (this == Null) return false;
            return Bounds.Contains(target);
        }
        public Vector3 ClosestPoint(Vector3 target) => Bounds.ClosestPoint(target);

        public static SoundRoom Convert(Transform transform, int type, float direct)
        {
            return new SoundRoom(type, direct, transform);
        }

        public override bool Equals(object other)
        {
            if (other is SoundRoom room)
            {
                return this == room;
            }

            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// 두 방을 합친 크기를 반환합니다.<br/>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static SoundRoom operator *(SoundRoom a, SoundRoom b)
        {
            Bounds tempBounds = a.Bounds;
            tempBounds.Encapsulate(b.Bounds);
            float direct = Mathf.Clamp(a.Direct + b.Direct, 0, 1);

            Vector3[] vertices = new Vector3[6];
            Vector3 center = tempBounds.center;
            // bottom
            vertices[0] = new Vector3(center.x, tempBounds.min.y, center.z);
            // left
            vertices[1] = new Vector3(tempBounds.min.x, center.y, center.z);
            // right
            vertices[2] = new Vector3(tempBounds.max.x, center.y, center.z);
            //forward
            vertices[3] = new Vector3(center.x, center.y, tempBounds.max.z);
            // backward
            vertices[4] = new Vector3(center.x, center.y, tempBounds.min.z);
            // upward
            vertices[5] = new Vector3(center.x, tempBounds.max.y, center.z);

            return new SoundRoom(a.BackgroundType | b.BackgroundType, tempBounds, direct, tempBounds.center, vertices);
        }
        public static implicit operator int(SoundRoom room) => room.Index;
    }
}
