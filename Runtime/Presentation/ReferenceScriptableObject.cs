using Syadeu.Database;
using UnityEngine;

namespace Syadeu.Presentation
{
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "NewObjectID", menuName = "CoreSystem/Presentation/Reference")]
#endif
    public sealed class ReferenceScriptableObject : ScriptableObject
    {
        [SerializeField] private ulong m_Hash = 0;

        public Reference Reference
        {
            get
            {
                if (m_Hash.Equals(0)) return Reference.Empty;
                return new Reference(m_Hash);
            }
            set
            {
                m_Hash = value.m_Hash;
            }
        }
    }
}
