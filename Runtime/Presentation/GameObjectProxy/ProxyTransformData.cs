using Syadeu.Collections;
using System;
using Unity.Mathematics;
using AABB = Syadeu.Collections.AABB;

namespace Syadeu.Presentation.Proxy
{
    internal struct ProxyTransformData : IEquatable<ProxyTransformData>
    {
        public bool m_IsOccupied;
        public bool m_EnableCull;
        public bool m_IsVisible;
        public bool m_DestroyQueued;

        private ClusterID m_ClusterID;
        public int m_Index;
        public int2 m_ProxyIndex;

        public Hash m_Hash;
        public int m_Generation;
        public PrefabReference m_Prefab;

        public float3 m_Translation;
        public float3 m_Scale;
        public float3 m_Center;
        public float3 m_Size;

        public quaternion m_Rotation;

        public bool m_GpuInstanced;

#pragma warning disable IDE1006 // Naming Styles
        public ClusterID clusterID
        {
            get
            {
                return m_ClusterID;
            }
            set
            {
                //if (!m_Prefab.IsNone() && m_Prefab.IsValid())
                //{
                //    $"in({m_Prefab.GetObjectSetting().m_Name}) from {m_ClusterID.GroupIndex}:{m_ClusterID.ItemIndex} -> {value.GroupIndex}:{value.ItemIndex}".ToLog();
                //}
                //else
                //{
                //    $"in from {m_ClusterID.GroupIndex}:{m_ClusterID.ItemIndex} -> {value.GroupIndex}:{value.ItemIndex}".ToLog();
                //}
                
                m_ClusterID = value;
            }
        }
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
