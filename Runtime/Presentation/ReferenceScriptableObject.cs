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
                m_Hash = value.Hash;
            }
        }

        public bool IsValid() => !m_Hash.Equals(0) && Reference.GetObject() != null;

        private bool Validate()
        {
            if (!PresentationSystem<DefaultPresentationGroup, EntitySystem>.IsValid() || !IsValid())
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
            return PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.CreateEntity(Reference, in position);
        }
        public Entity<IEntity> CreateEntity(in float3 position, in quaternion rotation, in float3 localScale)
        {
            if (!Validate())
            {
                throw new System.Exception();
            }
            return PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.CreateEntity(Reference, 
                in position, in rotation, in localScale);
        }
        public Entity<IObject> CreateObject()
        {
            if (!Validate())
            {
                throw new System.Exception();
            }
            return PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.CreateEntity(Reference);
        }
    }
}
