﻿// Copyright 2021 Seung Ha Kim
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

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;
using Unity.Collections;

namespace Syadeu.Presentation.Map
{
    [Obsolete]
    public struct old_GridDetectorComponent : IEntityComponent, IDisposable
    {
        internal EntityShortID m_MyShortID;
        internal int m_MaxDetectionRange;
        internal FixedList4096Bytes<int> m_ObserveIndices;
        internal GridLayerChain m_IgnoreLayers;
        
        internal FixedReferenceList64<EntityBase> m_TriggerOnly;
        internal bool m_TriggerOnlyInverse;

        internal FixedReferenceList64<TriggerPredicateAction> m_OnDetectedPredicate;
        internal FixedReferenceList64<TriggerPredicateAction> m_DetectRemoveCondition;
        internal FixedLogicTriggerAction8 m_OnDetected;

        /// <summary>
        /// 이 Detector 가 발견한 Entity 들을 담습니다.
        /// </summary>
        internal FixedList512Bytes<EntityShortID> m_Detected;
        /// <summary>
        /// 이 Detector 가 다른 Entity 의 Detector 에 의해 발견되었으면 해당 발견자를 담습니다.
        /// </summary>
        /// <remarks>
        /// GridDetector 를 상속받는 Entity 만 이 컴포넌트를 사용합니다.
        /// </remarks>
        internal FixedList512Bytes<EntityShortID> m_TargetedBy;

        public FixedList512Bytes<EntityShortID> Detected => m_Detected;
        public FixedList512Bytes<EntityShortID> TargetedBy => m_TargetedBy;

        /// <summary>
        /// 이 Detector 가 최대로 감시할 수 있는 시스템 연산상 그리드 인덱스의 최대치를 반환합니다.
        /// </summary>
        public int MaxDetectionIndicesCount => GridSizeComponent.CalculateMaxiumIndicesInRangeCount(m_MaxDetectionRange);

        void IDisposable.Dispose()
        {
            for (int i = 0; i < m_Detected.Length; i++)
            {
                var target = m_Detected[i].GetID().GetEntity<IEntity>();
                if (!target.HasComponent<old_GridDetectorComponent>())
                {
                    continue;
                }

                ref var targetDetector = ref target.GetComponent<old_GridDetectorComponent>();
                targetDetector.m_TargetedBy.Remove(m_MyShortID);
            }
            for (int i = 0; i < m_TargetedBy.Length; i++)
            {
                var target = m_TargetedBy[i].GetID().GetEntity<IEntity>();
                if (!target.HasComponent<old_GridDetectorComponent>()) continue;

                ref var targetDetector = ref target.GetComponent<old_GridDetectorComponent>();

                targetDetector.m_Detected.Remove(m_MyShortID);
            }

            m_ObserveIndices.Clear();
            m_Detected.Clear();
            m_TargetedBy.Clear();
        }

        public bool IsObserveIndex(in int gridIndex)
        {
            return m_ObserveIndices.Contains(gridIndex);
        }
    }
}