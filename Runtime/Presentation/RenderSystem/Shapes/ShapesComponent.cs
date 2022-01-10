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
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Render
{
#if CORESYSTEM_SHAPES
    public struct ShapesComponent : IEntityComponent
    {
        public enum Shape
        {
            Disc,
            Rectangle
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
        }
        public struct Offsets
        {
            public float3 position;
            public quaternion rotation;
        }
        public struct DiscParameters
        {
            public DiscType discType;
            public DiscGeometry discGeometry;
            public Angle angleStart, angleEnd;
            public DiscColors colors;
        }
        public struct RectangleParameters
        {
            public Rectangle.RectangleType type;
            public RectPivot pivot;
            public float2 size;
            public DashStyle dashStyle;

            public bool enableFill;
            public GradientFill fill;
        }

        internal ProxyTransform m_Transform;

        public ProxyTransform transform => m_Transform;

        public Shape shape;
        public Generals generals;
        public Offsets offsets;

        public DiscParameters discParameters;
        public RectangleParameters rectangleParameters; 

        public void Apply(ShapesPropertyBlock shapesProperty)
        {
            shape = shapesProperty.m_Shape;
            generals = new Generals
            {
                thickness = shapesProperty.m_Thickness,
            };

            offsets = new Offsets
            {
                position = 0,
                rotation = Quaternion.Euler(90, 0, 0)
            };

            switch (shapesProperty.m_Shape)
            {
                default:
                case Shape.Disc:
                    ApplyDiscParameters(shapesProperty);
                    break;
                case Shape.Rectangle:
                    ApplyRectangleParameters(shapesProperty);
                    break;
            }
        }

        private void ApplyDiscParameters(ShapesPropertyBlock shapesProperty)
        {
            discParameters = new DiscParameters
            {
                discType = shapesProperty.m_ArcParameters.m_DiscType,
                discGeometry = shapesProperty.m_ArcParameters.m_DiscGeometry,

                angleStart = new Angle(shapesProperty.m_ArcParameters.m_AngleDegreeStart * Mathf.Deg2Rad),
                angleEnd = new Angle(shapesProperty.m_ArcParameters.m_AngleDegreeEnd * Mathf.Deg2Rad),

                colors = DiscColors.Radial(shapesProperty.m_StartColor, shapesProperty.m_EndColor)
            };
        }
        private void ApplyRectangleParameters(ShapesPropertyBlock shapesProperty)
        {
            rectangleParameters = new RectangleParameters
            {
                type = shapesProperty.m_RectangleParameters.m_Type,
                pivot = shapesProperty.m_RectangleParameters.m_Pivot,
                size = shapesProperty.m_RectangleParameters.m_Size,
                dashStyle = shapesProperty.m_RectangleParameters.m_DashStyle,

                enableFill = shapesProperty.m_RectangleParameters.m_EnableFill
            };

            if (rectangleParameters.enableFill)
            {
                switch (shapesProperty.m_RectangleParameters.m_FillType)
                {
                    default:
                    case FillType.LinearGradient:
                        rectangleParameters.fill =
                            GradientFill.Linear(
                                start: shapesProperty.m_RectangleParameters.m_FillStart,
                                end: shapesProperty.m_RectangleParameters.m_FillEnd,

                                colorStart: shapesProperty.m_StartColor,
                                colorEnd: shapesProperty.m_EndColor,
                                space: shapesProperty.m_RectangleParameters.m_FillSpace);

                        break;
                    case FillType.RadialGradient:
                        rectangleParameters.fill =
                            GradientFill.Radial(
                                origin: shapesProperty.m_RectangleParameters.m_FillOrigin,
                                radius: shapesProperty.m_RectangleParameters.m_FillRadius,

                                colorInner: shapesProperty.m_StartColor,
                                colorOuter: shapesProperty.m_EndColor,
                                space: shapesProperty.m_RectangleParameters.m_FillSpace);
                        break;
                }
            }
            else rectangleParameters.fill = GradientFill.defaultFill;
        }
    }

    internal sealed class ShapesComponentProcessor : ComponentProcessor<ShapesComponent>
    {
        private GameObjectProxySystem m_ProxySystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, GameObjectProxySystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_ProxySystem = null;
        }
        private void Bind(GameObjectProxySystem other)
        {
            m_ProxySystem = other;
        }

        protected override void OnCreated(in InstanceID id, ref ShapesComponent com)
        {
            float3 pos;
            if (id.IsEntity<IEntity>())
            {
                Entity<IEntity> entity = id.GetEntity<IEntity>();
                ProxyTransform parent = entity.transform;
                pos = parent.position;
                com.m_Transform = m_ProxySystem.CreateTransform(pos, quaternion.identity, 1);

                com.m_Transform.SetParent(parent);
                com.m_Transform.localPosition = 0;
                //com.m_Transform.localEulerAngles = new float3(90, 0, 0);
            }
            else
            {
                pos = float3.zero;
                com.m_Transform = m_ProxySystem.CreateTransform(pos, quaternion.EulerZXY(90, 0, 0), 1);
            }
        }
        protected override void OnDestroy(in InstanceID entity, ref ShapesComponent com)
        {
            com.m_Transform.Destroy();
        }
    }
#endif
}