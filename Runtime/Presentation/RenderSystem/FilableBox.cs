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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Syadeu.Presentation.Render.UI
{
    public sealed class FilableBox : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<FilableBox, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_Value = new UxmlFloatAttributeDescription
            {
                name = "value",
                defaultValue = 50
            };

            UxmlFloatAttributeDescription m_MinimumFill = new UxmlFloatAttributeDescription
            {
                name = "minimum-fill",
                defaultValue = 0
            };
            UxmlFloatAttributeDescription m_MaximumFill = new UxmlFloatAttributeDescription
            {
                name = "maximum-fill",
                defaultValue = 100
            };
            UxmlColorAttributeDescription m_BackgroundColor = new UxmlColorAttributeDescription
            {
                name = "background-color",
                defaultValue = Color.clear
            };
            UxmlColorAttributeDescription m_FillColor = new UxmlColorAttributeDescription
            {
                name = "fill-color",
                defaultValue = Color.black
            };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                FilableBox ate = ve as FilableBox;

                ate.value = m_Value.GetValueFromBag(bag, cc);

                ate.minimumFill = m_MinimumFill.GetValueFromBag(bag, cc);
                ate.maximumFill = m_MaximumFill.GetValueFromBag(bag, cc);

                ate.backgroundColor = m_BackgroundColor.GetValueFromBag(bag, cc);
                ate.fillColor = m_FillColor.GetValueFromBag(bag, cc);
            }
        }

        private float m_Value = 50;

        private Color m_BackgroundColor = Color.clear;
        private Color m_FillColor = Color.black;

        public float minimumFill { get; set; }
        public float maximumFill { get; set; }

        public float value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                m_FillRoot.style.width = new StyleLength(new Length(m_Value / maximumFill * 100, LengthUnit.Percent));

                valueChanged?.Invoke(value);
            }
        }

        public Color backgroundColor
        {
            get => m_BackgroundColor;
            set
            {
                m_BackgroundColor = value;
                m_Root.style.backgroundColor = m_BackgroundColor;
            }
        }
        public Color fillColor
        {
            get => m_FillColor;
            set
            {
                m_FillColor = value;
                m_Fill.style.backgroundColor = m_FillColor;
            }
        }

        public event Action<float> valueChanged;

        private VisualElement m_Root, m_FillRoot, m_Fill;
        //private Label m_Text;

        public FilableBox() : this(0, 100, null) { }
        public FilableBox(float minFill, float maxFill, Action<float> valueChanged)
        {
            minimumFill = minFill;
            maximumFill = maxFill;

            this.valueChanged = valueChanged;

            m_Root = new VisualElement();
            m_Root.name = "Root";
            m_Root.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            m_Root.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            m_Root.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            m_Root.style.alignItems = new StyleEnum<Align>(Align.Center);
            this.Add(m_Root);

            //
            m_Root.style.backgroundColor = backgroundColor;

            m_FillRoot = new VisualElement();
            m_Fill = new VisualElement();
            m_FillRoot.Add(m_Fill);
            m_Root.Add(m_FillRoot);

            m_FillRoot.name = "Fill";
            m_FillRoot.style.overflow = new StyleEnum<Overflow>(Overflow.Hidden);
            m_FillRoot.style.width = new StyleLength(new Length(m_Value, LengthUnit.Percent));
            m_FillRoot.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            m_Fill.name = "Color";
            m_Fill.style.backgroundColor = m_FillColor;
            m_Fill.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            m_Fill.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

            //

            //m_Text = new Label($"{value} / {maximumFill} t");
            //m_Text.style.position = new StyleEnum<Position>(Position.Absolute);
            //m_Text.style.left = new StyleLength(new Length(70, LengthUnit.Percent));
            //m_Root.Add(m_Text);
        }
    }
}
