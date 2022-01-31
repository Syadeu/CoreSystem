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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    // https://github.com/Syadeu/CoreSystem/issues/87

    [DisplayName("Attribute: TRPG Grid Cover")]
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class TRPGGridCoverAttribute : AttributeBase,
        INotifyComponent<TRPGGridCoverComponent>
    {
        public sealed class DimensionInfo
        {
            [ReflectionSealedView]
            [JsonProperty(Order = 0, PropertyName = "Direction")]
            public Direction m_Direction = Direction.NONE;
            [UnityEngine.Tooltip(
                "오브젝트를 중심으로 얼마만큼 떨어져도 엄폐가능 타일로 표시할지 결정한다. " +
                "기본값은 1, 0보다 작을 수 없고, 0일 경우에는 이 방면으로는 엄폐가 불가능함을 암시적 선언")]
            [JsonProperty(Order = 1, PropertyName = "ForwardLength")]
            public int m_ForwardLength = 1;

            public DimensionInfo(Direction dir, int fLength)
            {
                m_Direction = dir;
                m_ForwardLength = fLength;
            }
        }

        [Description(
            "true 일 경우, 파괴 불가능한 오브젝트로 선언되며, 어떠한 환경요소에도 영향받지 않는다.")]
        [JsonProperty(Order = 0, PropertyName = "Immutable")]
        internal bool m_Immutable = false;
        [JsonProperty(Order = 1, PropertyName = "DimensionInfomations")]
        internal DimensionInfo[] m_DimensionInfomations = new DimensionInfo[4]
        {
            new DimensionInfo(Direction.Left, 1),
            new DimensionInfo(Direction.Right, 1),
            new DimensionInfo(Direction.Forward, 1),
            new DimensionInfo(Direction.Backward, 1)
        };
    }
    internal sealed class TRPGGridCoverAttributeProcessor : AttributeProcessor<TRPGGridCoverAttribute>
    {
        protected override void OnCreated(TRPGGridCoverAttribute attribute, Entity<IEntityData> entity)
        {
            ref TRPGGridCoverComponent component = ref entity.GetComponent<TRPGGridCoverComponent>();

            component.immutable = attribute.m_Immutable;
            for (int i = 0; i < 4; i++)
            {
                component.dimensions[i] = new TRPGGridCoverComponent.Dimension
                {
                    direction = attribute.m_DimensionInfomations[i].m_Direction,
                    forwardLength = attribute.m_DimensionInfomations[i].m_ForwardLength
                };
            }
        }
    }

    public struct TRPGGridCoverComponent : IEntityComponent
    {
        public struct Dimension
        {
            public Direction direction;
            public int forwardLength;
        }
        public struct Dimension4
        {
            public Dimension
                a, b, c, d;

            public Dimension this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => a,
                        1 => b,
                        2 => c,
                        3 => d,
                        _ => throw new IndexOutOfRangeException(),
                    };
                }
                set
                {
                    switch (index)
                    {
                        case 0: a = value; break;
                        case 1: b = value; break;
                        case 2: c = value; break;
                        case 3: d = value; break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
        }

        public bool immutable;
        public Dimension4 dimensions;
    }
}