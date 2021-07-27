using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization]
    public sealed class RenderSettings : StaticSettingEntity<RenderSettings>
    {
        public float4x4 m_ProjectionMatrix;
    }
}
