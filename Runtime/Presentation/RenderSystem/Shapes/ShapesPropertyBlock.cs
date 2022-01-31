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

#if CORESYSTEM_SHAPES
using Shapes;
#endif

using Syadeu.Collections;
using Newtonsoft.Json;
using UnityEngine;
using Unity.Mathematics;

namespace Syadeu.Presentation.Render
{
    public sealed class ShapesPropertyBlock : PropertyBlock<ShapesPropertyBlock>
    {
#if CORESYSTEM_SHAPES
        public sealed class DiscParameter : PropertyBlock<DiscParameter>
        {
            [JsonProperty(Order = 0, PropertyName = "Type")]
            public DiscType m_DiscType = DiscType.Arc;
            [JsonProperty(Order = 1, PropertyName = "DiscGeometry")]
            internal DiscGeometry m_DiscGeometry = DiscGeometry.Flat2D;

            [JsonProperty(Order = 2, PropertyName = "AngleDegreeStart")]
            internal float m_AngleDegreeStart = 0;
            [JsonProperty(Order = 3, PropertyName = "AngleDegreeEnd")]
            internal float m_AngleDegreeEnd = 360;
        }
        public sealed class RectangleParameter : PropertyBlock<RectangleParameter>
        {
            [JsonProperty(Order = 0, PropertyName = "Type")]
            public Rectangle.RectangleType m_Type;
            [JsonProperty(Order = 1, PropertyName = "Pivot")]
            public RectPivot m_Pivot;
            [JsonProperty(Order = 2, PropertyName = "Size")]
            public float2 m_Size;
            [JsonProperty(Order = 3, PropertyName = "DashStyle")]
            public DashStyle m_DashStyle;

            [Space]
            [JsonProperty(Order = 4, PropertyName = "EnableFill")]
            public bool m_EnableFill = false;
            [JsonProperty(Order = 5, PropertyName = "FillType")]
            public FillType m_FillType = FillType.LinearGradient;
            [JsonProperty(Order = 6, PropertyName = "FillSpace")]
            public FillSpace m_FillSpace = FillSpace.Local;

            [Header("Linear Fill")]
            [JsonProperty(Order = 7, PropertyName = "FillStart")]
            public float3 m_FillStart = 0;
            [JsonProperty(Order = 8, PropertyName = "FillEnd")]
            public float3 m_FillEnd = new float3(0, 1, 0);

            [Header("Radial Fill")]
            [JsonProperty(Order = 9, PropertyName = "FillOrigin")]
            public float3 m_FillOrigin = 0;
            [JsonProperty(Order = 10, PropertyName = "FillRadius")]
            public float m_FillRadius = 5;
        }

        [JsonProperty(Order = 0, PropertyName = "EnableShapes")]
        internal bool m_EnableShapes = false;
        [JsonProperty(Order = 1, PropertyName = "Shape")]
        internal ShapesComponent.Shape m_Shape;

        [Space, Header("Generals")]
        [JsonProperty(Order = 2, PropertyName = "Thickness")]
        internal float m_Thickness = 1;
        
        [JsonProperty(Order = 3, PropertyName = "StartColor")]
        internal Color m_StartColor;
        [JsonProperty(Order = 4, PropertyName = "EndColor")]
        internal Color m_EndColor;

        [Space]
        [JsonProperty(Order = 5, PropertyName = "ArcParameters")]
        internal DiscParameter m_ArcParameters = new DiscParameter();
        [JsonProperty(Order = 6, PropertyName = "RectangleParameters")]
        internal RectangleParameter m_RectangleParameters = new RectangleParameter();

        [JsonIgnore]
        public bool EnableShapes => m_EnableShapes;
#endif
    }
}