using Syadeu.Presentation.Entities;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public readonly struct RaycastInfo
    {
        public readonly Entity<IEntity> entity;
        public readonly bool hit;
        
        public readonly float distance;
        public readonly float3 point;

        internal RaycastInfo(Entity<IEntity> a, bool b, float c, float3 d)
        {
            entity = a; hit = b; distance = c; point = d;
        }
    }
}
