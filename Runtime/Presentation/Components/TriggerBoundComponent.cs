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
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Unity.Mathematics;

namespace Syadeu.Presentation.Entities
{
    public struct TriggerBoundComponent : IEntityComponent
    {
        private bool m_Enable;

        private FixedReferenceList16<EntityBase> m_TriggerOnly;
        private bool m_Inverse;

        private float3 
            m_Center, m_Size;

        private FixedReferenceList64<TriggerAction>
            m_OnTriggerEnter, m_OnTriggerExit;

        private FixedReference<TriggerBoundLayer> m_Layer;

        private FixedInstanceList64<IEntity> m_Triggered;

        public float3 Center
        {
            get => m_Center;
            set => m_Center = value;
        }
        public float3 Size
        {
            get => m_Size;
            set => m_Size = value;
        }

        internal TriggerBoundComponent(in TriggerBoundAttribute att)
        {
            m_Enable = true;

            m_TriggerOnly = att.m_TriggerOnly.ToFixedList16();
            m_Inverse = att.m_Inverse;

            m_Center = att.m_Center;
            m_Size = att.m_Size;

            m_OnTriggerEnter = att.m_OnTriggerEnter.ToFixedList64();
            m_OnTriggerExit = att.m_OnTriggerExit.ToFixedList64();

            m_Layer = att.m_Layer;

            m_Triggered = new FixedInstanceList64<IEntity>();
        }

        internal void ScheduleOnTriggerEnter(in IEntityDataID entity)
        {
            m_OnTriggerEnter.Schedule(entity);
        }
        internal void ScheduleOnTriggerExit(in IEntityDataID entity)
        {
            m_OnTriggerExit.Schedule(entity);
        }
    }
}
