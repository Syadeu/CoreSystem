using System;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Proxy
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

        public TRS Project(TRS parent)
        {
            quaternion targetRot = math.mul(parent.m_Rotation, m_Rotation);

            return new TRS
            {
                m_Position = parent.m_Position + math.mul(targetRot, m_Position),
                m_Rotation = targetRot,
                m_Scale = math.mul(parent.m_Scale, m_Scale)
            };
        }
    }
}
