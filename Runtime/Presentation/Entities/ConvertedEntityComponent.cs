using Syadeu.Presentation.Proxy;
using UnityEngine;

namespace Syadeu.Presentation.Entities
{
    public sealed class ConvertedEntityComponent : MonoBehaviour
    {
        internal ConvertedEntity m_Entity;

        public Entity<ConvertedEntity> Entity => m_Entity;

        public new ITransform transform => m_Entity.transform;
    }
}
