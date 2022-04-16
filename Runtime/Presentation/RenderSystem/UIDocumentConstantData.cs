// Copyright 2022 Seung Ha Kim
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
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Render
{
    [DisplayName("ConstantData: UI Document Constant Data")]
    public sealed class UIDocumentConstantData : ConstantData
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "PanelSettings")]
        internal PrefabReference<PanelSettings> m_PanelSettings = PrefabReference<PanelSettings>.None;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "UXMLAsset")]
        internal PrefabReference<VisualTreeAsset> m_UXMLAsset = PrefabReference<VisualTreeAsset>.None;

        [SerializeField, JsonProperty(Order = 2, PropertyName = "CreateOnLoad")]
        internal bool m_CreateOnLoad = false;
        [SerializeField, JsonProperty(Order = 3, PropertyName = "CreateWithOpen")]
        internal bool m_CreateWithOpen = true;

#if ENABLE_INPUT_SYSTEM
        [Header("InputSystem")]
        [SerializeField, JsonProperty(Order = 4, PropertyName = "BindInputSystem")]
        internal bool m_BindInputSystem = false;
        [SerializeField, JsonProperty(Order = 5, PropertyName = "InputAction")]
        internal InputAction m_InputAction;
#endif
    }
    internal sealed class UIDocumentConstantDataProcessor : EntityProcessor<UIDocumentConstantData>
    {
        protected override void OnCreated(UIDocumentConstantData obj)
        {
            if (!obj.m_UXMLAsset.IsValid()) return;

            if (obj.m_CreateOnLoad)
            {
                ScreenCanvasSystem.CreateVisualElement(obj.m_PanelSettings, obj.m_UXMLAsset);
            }
        }
    }
}
