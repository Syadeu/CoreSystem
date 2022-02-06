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
using Unity.Collections;

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
        [ReflectionSealedView]
        [JsonProperty(Order = 1, PropertyName = "DimensionInfomations")]
        internal DimensionInfo[] m_DimensionInfomations = new DimensionInfo[4]
        {
            new DimensionInfo((Direction)(1 << 2), 1),
            new DimensionInfo((Direction)(1 << 3), 1),
            new DimensionInfo((Direction)(1 << 4), 1),
            new DimensionInfo((Direction)(1 << 5), 1)
        };
    }
    internal sealed class TRPGGridCoverAttributeProcessor : AttributeProcessor<TRPGGridCoverAttribute>
    {
        protected override void OnCreated(TRPGGridCoverAttribute attribute, Entity<IEntityData> entity)
        {
            ref TRPGGridCoverComponent component = ref entity.GetComponent<TRPGGridCoverComponent>();

            component.immutable = attribute.m_Immutable;
            TRPGGridCoverComponent.Dimension4 dimensions = new TRPGGridCoverComponent.Dimension4();
            for (int i = 0; i < 4; i++)
            {
                dimensions[(Direction)(1 << (i + 2))] = new TRPGGridCoverComponent.Dimension
                {
                    direction = attribute.m_DimensionInfomations[i].m_Direction,
                    forwardLength = attribute.m_DimensionInfomations[i].m_ForwardLength
                };
            }
            component.dimensions = dimensions;

            "asdasd".ToLog();
        }
    }

    public struct TRPGGridCoverComponent : IEntityComponent
    {
        [BurstCompatible]
        public struct Dimension
        {
            public Direction direction;
            public int forwardLength;
        }
        [BurstCompatible]
        public struct Dimension4
        {
            public Dimension
                a, b, c, d;

            public Dimension this[Direction dir]
            {
                get
                {
                    return dir switch
                    {
                        Direction.Left => a,
                        Direction.Right => b,
                        Direction.Forward => c,
                        Direction.Backward => d,
                        _ => throw new IndexOutOfRangeException($"{dir}"),
                    };
                }
                set
                {
                    switch (dir)
                    {
                        case Direction.Left: a = value; break;
                        case Direction.Right: b = value; break;
                        case Direction.Forward: c = value; break;
                        case Direction.Backward: d = value; break;
                        default:
                            throw new IndexOutOfRangeException($"{dir}");
                    }
                }
            }
        }

        public bool immutable;
        public Dimension4 dimensions;
    }
}