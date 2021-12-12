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

namespace Syadeu.Presentation.Render
{
    public sealed class ShapesPropertyBlock : PropertyBlock<ShapesPropertyBlock>
    {
#if CORESYSTEM_SHAPES
        public sealed class ArcShapeParameter : PropertyBlock<ArcShapeParameter>
        {
            [JsonProperty(Order = 0, PropertyName = "AngleDegreeStart")]
            internal float m_AngleDegreeStart = 0;
            [JsonProperty(Order = 1, PropertyName = "AngleDegreeEnd")]
            internal float m_AngleDegreeEnd = 360;
        }

        [JsonProperty(Order = 0, PropertyName = "EnableShapes")]
        internal bool m_EnableShapes = false;
        [JsonProperty(Order = 1, PropertyName = "Shape")]
        internal ShapesComponent.Shape m_Shape;

        [Space, Header("Generals")]
        [JsonProperty(Order = 2, PropertyName = "Thickness")]
        internal float m_Thickness = 1;
        [JsonProperty(Order = 3, PropertyName = "DiscGeometry")]
        internal DiscGeometry m_DiscGeometry = DiscGeometry.Flat2D;
        [JsonProperty(Order = 4, PropertyName = "StartColor")]
        internal Color m_StartColor;
        [JsonProperty(Order = 5, PropertyName = "EndColor")]
        internal Color m_EndColor;

        [Space]
        [JsonProperty(Order = 6, PropertyName = "ArcParameters")]
        internal ArcShapeParameter m_ArcParameters = new ArcShapeParameter();

        [JsonIgnore]
        public bool EnableShapes => m_EnableShapes;
#endif
    }
}