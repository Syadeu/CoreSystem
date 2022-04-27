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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using System;
using UnityEngine;

namespace Syadeu.Presentation
{
    [Serializable]
    public sealed class EntityEventProperty : PropertyBlock<EntityEventProperty>
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "OnCreatedConstAction")]
        private ConstActionReferenceArray m_OnCreatedConstAction = ConstActionReferenceArray.Empty;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "OnDestroyConstAction")]
        private ConstActionReferenceArray m_OnDestroyConstAction = ConstActionReferenceArray.Empty;

        [Space]
        [SerializeField, JsonProperty(Order = 2, PropertyName = "OnCreatedTriggerAction")]
        private ArrayWrapper<Reference<TriggerAction>> m_OnCreatedTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;
        [SerializeField, JsonProperty(Order = 3, PropertyName = "OnDestroyTriggerAction")]
        private ArrayWrapper<Reference<TriggerAction>> m_OnDestroyTriggerAction = ArrayWrapper<Reference<TriggerAction>>.Empty;

        public void ExecuteOnCreated(InstanceID entity)
        {
            m_OnCreatedConstAction.Execute(entity);
            m_OnCreatedTriggerAction.Execute(entity);
        }
        public void ExecuteOnDestroy(InstanceID entity)
        {
            m_OnDestroyConstAction.Execute(entity);
            m_OnDestroyTriggerAction.Execute(entity);
        }
    }
}
