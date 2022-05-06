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
using UnityEngine.UIElements;

namespace Syadeu.Presentation.TurnTable.UI
{
    [DisplayName("ConstantData: TRPG Inventory Item UI Constant Data")]
    public sealed class TRPGInventoryItemUIConstantData : ConstantData, IPrefabPreloader
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "UXMLAsset")]
        internal PrefabReference<VisualTreeAsset> m_UXMLAsset = PrefabReference<VisualTreeAsset>.None;

        [SerializeField, JsonProperty(Order = 1, PropertyName = "TitleNameField")]
        internal string m_TitleNameField = "Name";
        [SerializeField, JsonProperty(Order = 2, PropertyName = "QuantityField")]
        internal string m_QuantityField = "Quantity";

        void IPrefabPreloader.Register(PrefabPreloader loader)
        {
            loader.Add(m_UXMLAsset);
        }

        public TemplateContainer GetRoot()
        {
            var temp = m_UXMLAsset.Asset;
            TemplateContainer root = temp.CloneTree();

            Label titleNameField = root.Q<Label>(name: m_TitleNameField);
            titleNameField.text = "This is a test";

            Label quantityField = root.Q<Label>(name: m_QuantityField);
            quantityField.text = "x9999";

            return root;
        }
    }
    internal sealed class TRPGInventoryItemUIConstantDataProcessor : EntityProcessor<TRPGInventoryItemUIConstantData>
    {
        //protected override void OnCreated(TRPGInventoryItemUIConstantData obj)
        //{
        //    var temp = obj.m_UXMLAsset.Asset;
        //    TemplateContainer root = temp.CloneTree();

        //    Label titleNameField = root.Q<Label>(name: obj.m_TitleNameField);
        //    titleNameField.text = "This is a test";

        //    Label quantityField = root.Q<Label>(name: obj.m_QuantityField);
        //    quantityField.text = "x9999";
        //}
    }
}