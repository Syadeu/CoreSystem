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
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Actions;
using System.ComponentModel;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.TurnTable.UI
{
    [DisplayName("Data: TRPG Shortcut data")]
    public sealed class TRPGShortcutData : ConstantData
    {
        [JsonProperty(Order = 0, PropertyName = "Order")]
        public int m_Order;
        [JsonProperty(Order = 1, PropertyName = "Image")]
        public PrefabReference<Texture2D> m_Image = PrefabReference<Texture2D>.None;

        [JsonProperty(Order = 2, PropertyName = "VisibleOptions")]
        public Reference<TriggerPredicateAction>[] m_VisibleOptions = Array.Empty<Reference<TriggerPredicateAction>>();

        [Space]
        [JsonProperty(Order = 3, PropertyName = "OnEnable")]
        public Reference<InstanceAction>[] m_OnEnable = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 4, PropertyName = "OnTargetEnable")]
        public Reference<TriggerAction>[] m_OnTargetEnable = Array.Empty<Reference<TriggerAction>>();

        [Space]
        [JsonProperty(Order = 5, PropertyName = "OnDisable")]
        public Reference<InstanceAction>[] m_OnDisable = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 6, PropertyName = "OnTargetDisable")]
        public Reference<TriggerAction>[] m_OnTargetDisable = Array.Empty<Reference<TriggerAction>>();
    }
    internal sealed class TRPGShortcutDataProcessor : EntityProcessor<TRPGShortcutData>
    {
        private readonly List<TRPGShortcutData> m_Data = new List<TRPGShortcutData>();

        public IReadOnlyList<TRPGShortcutData> Data => m_Data;

        protected override void OnCreated(TRPGShortcutData obj)
        {
            m_Data.Add(obj);
        }
    }
}