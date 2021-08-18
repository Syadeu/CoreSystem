using Syadeu.Database;
using System;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    internal struct ProxyTransformData : IEquatable<ProxyTransformData>
    {
        internal bool m_IsOccupied;
        internal bool m_EnableCull;
        internal bool m_IsVisible;
        internal bool m_DestroyQueued;

        internal ClusterID m_ClusterID;
        internal int m_Index;
        internal int2 m_ProxyIndex;

        internal Hash m_Hash;
        internal int m_Generation;
        internal PrefabReference m_Prefab;

        internal float3 m_Translation;
        internal float3 m_Scale;
        internal float3 m_Center;
        internal float3 m_Size;

        internal quaternion m_Rotation;

#pragma warning disable IDE1006 // Naming Styles
        public bool destroyed
        {
            get
            {
                if (!m_IsOccupied || m_DestroyQueued) return true;
                return false;
            }
        }
        public float3 translation
        {
            get => m_Translation;
            set => m_Translation = value;
        }
        public quaternion rotation
        {
            get => m_Rotation;
            set => m_Rotation = value;
        }
        public float3 scale
        {
            get => m_Scale;
            set => m_Scale = value;
        }
        //public AABB aabb => new AABB(m_Center + m_Translation, m_Size).Rotation(m_Rotation);
#pragma warning restore IDE1006 // Naming Styles

        public AABB GetAABB()
        {
            return new AABB(m_Center + m_Translation, m_Size).Rotation(in m_Rotation, in m_Scale);
        }
        public bool Equals(ProxyTransformData other) => m_Generation.Equals(other.m_Generation);
    }
}
