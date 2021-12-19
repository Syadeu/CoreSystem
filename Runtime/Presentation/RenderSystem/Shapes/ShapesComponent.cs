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

using Syadeu.Presentation.Proxy;
using Unity.Mathematics;
using Syadeu.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
#if CORESYSTEM_SHAPES
    public struct ShapesComponent : IEntityComponent
    {
        public enum Shape
        {
            Arc
        }
        public struct Angle
        {
            public float radian;

            public float degree
            {
                get => UnityEngine.Mathf.Rad2Deg * radian;
                set => radian = UnityEngine.Mathf.Deg2Rad * value;
            }
            public float turn
            {
                set => radian = ShapesMath.TAU * value;
            }

            public Angle(float radian)
            {
                this.radian = radian;
            }
        }
        public struct Generals
        {
            public float thickness;
            public DiscGeometry discGeometry;
            public DiscColors colors;
        }
        public struct Offsets
        {
            public float3 position;
            public quaternion rotation;
        }
        public struct ArcParameters
        {
            public Angle angleStart, angleEnd;
        }

        internal ProxyTransform m_Transform;

        public ProxyTransform transform => m_Transform;

        public Shape shape;
        public Generals generals;
        public Offsets offsets;

        public ArcParameters arcParameters;

        public void Apply(ShapesPropertyBlock shapesProperty)
        {
            shape = shapesProperty.m_Shape;
            generals = new Generals
            {
                thickness = shapesProperty.m_Thickness,
                discGeometry = shapesProperty.m_DiscGeometry,
                colors = DiscColors.Radial(shapesProperty.m_StartColor, shapesProperty.m_EndColor)
            };

            offsets = new Offsets
            {
                position = 0,
                rotation = Quaternion.Euler(90, 0, 0)
            };

            arcParameters = new ArcParameters
            {
                angleStart = new Angle(shapesProperty.m_ArcParameters.m_AngleDegreeStart * Mathf.Deg2Rad),
                angleEnd = new Angle(shapesProperty.m_ArcParameters.m_AngleDegreeEnd * Mathf.Deg2Rad),
            };
        }
    }
#endif
}