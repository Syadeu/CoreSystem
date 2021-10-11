using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "NewObjectID", menuName = "CoreSystem/Presentation/Reference")]
#endif
    public sealed class ReferenceScriptableObject : ScriptableObject, IValidation
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

        public bool IsValid() => !m_Hash.Equals(0) && Reference.GetObject() != null;

        private bool Validate()
        {
            if (!PresentationSystem<EntitySystem>.IsValid() || !IsValid())
            {
                return false;
            }
            return true;
        }
        public Entity<IEntity> CreateEntity(in float3 position)
        {
            if (!Validate())
            {
                throw new System.Exception();
            }
            return PresentationSystem<EntitySystem>.System.CreateEntity(Reference, in position);
        }
        public Entity<IEntity> CreateEntity(in float3 position, in quaternion rotation, in float3 localScale)
        {
            if (!Validate())
            {
                throw new System.Exception();
            }
            return PresentationSystem<EntitySystem>.System.CreateEntity(Reference, 
                in position, in rotation, in localScale);
        }
        public EntityData<IEntityData> CreateObject()
        {
            if (!Validate())
            {
                throw new System.Exception();
            }
            return PresentationSystem<EntitySystem>.System.CreateObject(Reference);
        }
    }
}
