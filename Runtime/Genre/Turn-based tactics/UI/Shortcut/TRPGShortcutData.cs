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
using Unity.Mathematics;
using TMPro;

namespace Syadeu.Presentation.TurnTable.UI
{
    [DisplayName("Data: TRPG Shortcut data")]
    public sealed class TRPGShortcutData : ConstantData, IComparable<TRPGShortcutData>
    {
        [Serializable]
        public sealed class GeneralOptions : PropertyBlock<GeneralOptions>
        {
            [Space]
            [JsonProperty(Order = 0, PropertyName = "SizeDelta")]
            public float2 m_SizeDelta = new float2(40, 40);
            [JsonProperty(Order = 1, PropertyName = "TextureOffset")]
            public float m_TextureOffset = 2.5f;

            [Space, Header("Image")]
            [JsonProperty(Order = 2, PropertyName = "BackgroundImage")]
            public PrefabReference<Sprite> m_BackgroundImage = PrefabReference<Sprite>.None;
            [JsonProperty(Order = 3, PropertyName = "BackgroundColor")]
            public Color32 m_BackgroundColor = Color.white;
            [JsonProperty(Order = 4, PropertyName = "Image")]
            public PrefabReference<Sprite> m_Image = PrefabReference<Sprite>.None;
            [JsonProperty(Order = 5, PropertyName = "ImageColor")]
            public Color32 m_ImageColor = Color.white;

            [Space]
            [JsonProperty(Order = 6, PropertyName = "Font")]
            public PrefabReference<TMP_FontAsset> m_Font = PrefabReference<TMP_FontAsset>.None;
            [JsonProperty(Order = 7, PropertyName = "FontSize")]
            public float m_FontSize = 18;
        }
        [Serializable]
        public sealed class VisibleOptions : PropertyBlock<VisibleOptions>
        {
            [Space]
            [JsonProperty(Order = 0, PropertyName = "VisibleOptions")]
            public Reference<TriggerPredicateAction>[] m_VisibleOptions = Array.Empty<Reference<TriggerPredicateAction>>();
            [JsonProperty(Order = 1, PropertyName = "ConstVisibleOptions")]
            public ConstActionReference<bool>[] m_ConstVisibleOptions = Array.Empty<ConstActionReference<bool>>();
        }
        [Serializable]
        public sealed class OnEnableOptions : PropertyBlock<OnEnableOptions>
        {
            [Space]
            [JsonProperty(Order = 0, PropertyName = "OnEnableConst")]
            public ConstActionReference[] m_OnEnableConst = Array.Empty<ConstActionReference>();
            [JsonProperty(Order = 1, PropertyName = "OnEnable")]
            public Reference<InstanceAction>[] m_OnEnable = Array.Empty<Reference<InstanceAction>>();
            [JsonProperty(Order = 2, PropertyName = "OnTargetEnable")]
            [Description("대상은 현재 턴의 엔티티입니다.")]
            public Reference<TriggerAction>[] m_OnTargetEnable = Array.Empty<Reference<TriggerAction>>();
        }
        [Serializable]
        public sealed class OnDisableOptions : PropertyBlock<OnDisableOptions>
        {
            [Space]
            [JsonProperty(Order = 0, PropertyName = "OnDisableConst")]
            public ConstActionReference[] m_OnDisableConst = Array.Empty<ConstActionReference>();
            [JsonProperty(Order = 1, PropertyName = "OnDisable")]
            public Reference<InstanceAction>[] m_OnDisable = Array.Empty<Reference<InstanceAction>>();
            [JsonProperty(Order = 2, PropertyName = "OnTargetDisable")]
            [Description("대상은 현재 턴의 엔티티입니다.")]
            public Reference<TriggerAction>[] m_OnTargetDisable = Array.Empty<Reference<TriggerAction>>();
        }
        [Serializable]
        public sealed class OnExecuteOptions : PropertyBlock<OnExecuteOptions>
        {
            [Space]
            [Description(
                "true 일 경우, 이 버튼이 실제로 동작하고 난 직후 비활성화 됩니다.")]
            [JsonProperty(Order = 0, PropertyName = "DisableOnExecute")]
            public bool m_DisableOnExecute = false;

            [Space]
            [JsonProperty(Order = 1, PropertyName = "OnExecuteConst")]
            public ConstActionReference[] m_OnExecuteConst = Array.Empty<ConstActionReference>();
            [JsonProperty(Order = 2, PropertyName = "OnExecute")]
            public Reference<InstanceAction>[] m_OnExecute = Array.Empty<Reference<InstanceAction>>();
            [JsonProperty(Order = 3, PropertyName = "OnTargetExecute")]
            [Description("대상은 현재 턴의 엔티티입니다.")]
            public Reference<TriggerAction>[] m_OnTargetExecute = Array.Empty<Reference<TriggerAction>>();
        }

        [Description("왼쪽으로부터 몇번째 순서에 위치할지 결정합니다.")]
        [JsonProperty(Order = 0, PropertyName = "Order")]
        public int m_Order;
        [JsonProperty(Order = 1, PropertyName = "Generals")]
        public GeneralOptions m_Generals = new GeneralOptions();

        [Space]
        [JsonProperty(Order = 2, PropertyName = "VisibleOptions")]
        public VisibleOptions m_VisibleOptions = new VisibleOptions();

        [Space]
        [JsonProperty(Order = 4, PropertyName = "OnEnableOptions")]
        public OnEnableOptions m_OnEnableOptions = new OnEnableOptions();

        [Space]
        [JsonProperty(Order = 7, PropertyName = "OnDisableOptions")]
        public OnDisableOptions m_OnDisableOptions = new OnDisableOptions();

        [Space]
        [JsonProperty(Order = 10, PropertyName = "OnExecuteOptions")]
        public OnExecuteOptions m_OnExecuteOptions = new OnExecuteOptions();

        int IComparable<TRPGShortcutData>.CompareTo(TRPGShortcutData other)
        {
            if (m_Order < other.m_Order) return -1;
            else if (m_Order > other.m_Order) return 1;
            return 0;
        }

        protected override ObjectBase Copy()
        {
            TRPGShortcutData other = (TRPGShortcutData)base.Copy();



            return other;
        }
    }
    internal sealed class TRPGShortcutDataProcessor : EntityProcessor<TRPGShortcutData>
    {
        private static TRPGShortcutDataProcessor s_Instance;
        private readonly List<TRPGShortcutData> m_Data = new List<TRPGShortcutData>();

        public static IReadOnlyList<TRPGShortcutData> Data => s_Instance?.m_Data;

        protected override void OnInitialize()
        {
            s_Instance = this;
        }
        protected override void OnCreated(TRPGShortcutData obj)
        {
            s_Instance = this;

            m_Data.Add(obj);
            m_Data.Sort();
        }
        protected override void OnDispose()
        {
            s_Instance = null;
        }
    }
}