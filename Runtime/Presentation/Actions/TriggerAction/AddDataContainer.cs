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
using Syadeu.Internal;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Add DataContainer")]
    [ReflectionDescription("" +
        "이 Entity 를 해당 키 값으로 등록합니다. " +
        "Type 은 EntityData<IEntityData> 로 등록됩니다.")]
    public sealed class AddDataContainer : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Key")]
        private string m_Key = string.Empty;

        [JsonIgnore] private DataContainerSystem m_DataContainer;
        [JsonIgnore] private Hash m_KeyHash = Hash.Empty;

        protected override ObjectBase Copy()
        {
            AddDataContainer action = (AddDataContainer)base.Copy();
            action.m_Key = string.Copy(m_Key);

            return action;
        }
        protected override void OnCreated()
        {
            m_DataContainer = PresentationSystem<DefaultPresentationGroup, DataContainerSystem>.System;
            if (!string.IsNullOrEmpty(m_Key))
            {
                m_KeyHash = DataContainerSystem.ToDataHash(m_Key);
            }
            else
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"{nameof(AddDataContainer)}({Name}) error. Key({m_Key}) cannot be a null or empty.");
            }

            $"asdasdasd iu Key :: {m_Key} :: {m_KeyHash}".ToLog();
        }
        protected override void OnDestroy()
        {
            m_DataContainer = null;
        }
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            if (m_KeyHash.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"{nameof(AddDataContainer)}({Name}) error. Key({m_Key}) cannot be a null or empty.");
                return;
            }

            m_DataContainer.Enqueue(m_KeyHash, entity);
        }
    }
}
