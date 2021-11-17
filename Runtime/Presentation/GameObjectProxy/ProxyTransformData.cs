// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Syadeu.Collections;
using System;
using Unity.Collections;
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

        #region Hierarchy

        public int m_ParentIndex;
        public FixedList128Bytes<int> m_ChildIndices;

        #endregion

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

        public float4x4 localToWorld
        {
            get => float4x4.TRS(m_Translation, m_Rotation, m_Scale);
        }
        public float4x4 worldToLocal
        {
            get => math.inverse(localToWorld);
        }

#pragma warning restore IDE1006 // Naming Styles

        public AABB GetAABB()
        {
            return new AABB(m_Center + m_Translation, m_Size).Rotation(in m_Rotation, in m_Scale);
        }
        public bool Equals(ProxyTransformData other) => m_Generation.Equals(other.m_Generation);
    }
}
