using Newtonsoft.Json;
using Syadeu.Database.Converters;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Database
{
    [StructLayout(LayoutKind.Sequential)]
    [JsonConverter(typeof(AABBJsonConverter))]
    public struct AABB
    {
        internal float3 m_Center;
        internal float3 m_Extents;

        public AABB(float3 center, float3 size)
        {
            m_Center = center;
            m_Extents = size * .5f;
        }
        public AABB(int3 center, int3 size)
        {
            m_Center = new float3(center);
            m_Extents = new float3(size) * .5f;
        }

#pragma warning disable IDE1006 // Naming Styles
        [JsonIgnore] public Vector3 center { get { return m_Center; } set { m_Center = value; } }
        [JsonIgnore] public Vector3 size { get { return m_Extents * 2.0F; } set { m_Extents = value * 0.5F; } }
        [JsonIgnore] public Vector3 extents { get { return m_Extents; } set { m_Extents = value; } }
        [JsonIgnore] public Vector3 min { get { return center - extents; } set { SetMinMax(value, max); } }
        [JsonIgnore] public Vector3 max { get { return center + extents; } set { SetMinMax(min, value); } }
#pragma warning restore IDE1006 // Naming Styles
        public void SetMinMax(Vector3 min, Vector3 max)
        {
            extents = (max - min) * 0.5F;
            center = min + extents;
        }
    }
}
