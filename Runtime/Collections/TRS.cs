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

using Syadeu.Collections.Proxy;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Collections
{
    [Serializable]
    public struct TRS
    {
        public float3 m_Position;
        public quaternion m_Rotation;
        public float3 m_Scale;

        public TRS(float3 pos, quaternion rot, float3 scale)
        {
            m_Position = pos;
            m_Rotation = rot;
            m_Scale = scale;
        }
        public TRS(float3 pos, float3 eulerAngles, float3 scale)
        {
            m_Position = pos;
            m_Rotation = quaternion.EulerZXY(eulerAngles * Mathf.Deg2Rad);
            m_Scale = scale;
        }
        public TRS(ITransform tr)
        {
            m_Position = tr.position;
            m_Rotation = tr.rotation;
            m_Scale = tr.scale;
        }
        public TRS(Transform tr)
        {
            m_Position = tr.position;
            m_Rotation = tr.rotation;
            m_Scale = tr.localScale;
        }

        public float4x4 LocalToWorldMatrix => float4x4.TRS(m_Position, m_Rotation, m_Scale);

        public TRS Project(TRS parent)
        {
            quaternion targetRot = math.mul(parent.m_Rotation, m_Rotation);
            float4x4 local2world = parent.LocalToWorldMatrix;

            return new TRS
            {
                m_Position = math.mul(local2world, new float4(m_Position, 1)).xyz,
                m_Rotation = targetRot,
                m_Scale = math.mul(parent.m_Scale, m_Scale)
            };
        }
    }
}
