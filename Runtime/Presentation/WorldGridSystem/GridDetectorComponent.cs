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

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Unity.Collections;

namespace Syadeu.Presentation.Grid
{
    public struct GridDetectorComponent : IEntityComponent
    {
        private int m_DetectionRange;

        internal FixedList4096Bytes<GridIndex> m_ObserveIndices;

        internal FixedReferenceList64<EntityBase> m_TriggerOnly;
        internal bool m_TriggerOnlyInverse;

        internal FixedReferenceList64<TriggerPredicateAction> m_OnDetectedPredicate;
        internal FixedReferenceList64<TriggerPredicateAction> m_DetectRemoveCondition;
        internal FixedLogicTriggerAction8 m_OnDetected;

        /// <summary>
        /// 이 Detector 가 발견한 Entity 들을 담습니다.
        /// </summary>
        internal FixedList512Bytes<InstanceID> m_Detected;
        /// <summary>
        /// 이 Detector 가 다른 Entity 의 Detector 에 의해 발견되었으면 해당 발견자를 담습니다.
        /// </summary>
        internal FixedList512Bytes<InstanceID> m_TargetedBy;

        public int DetectedRange { get => m_DetectionRange; set => m_DetectionRange = value; }
        /// <summary>
        /// 이 Detector 가 최대로 감시할 수 있는 시스템 연산상 그리드 인덱스의 최대치를 반환합니다.
        /// </summary>
        public int MaxDetectionIndicesCount => CalculateMaxiumIndicesInRangeCount(m_DetectionRange);

        private static int CalculateMaxiumIndicesInRangeCount(in int range)
        {
            int height = ((range * 2) + 1);
            return height * height * height;
        }
    }
    internal sealed class GridDetectorComponentProcessor : ComponentProcessor<GridDetectorComponent>
    {
        protected override void OnInitialize()
        {
            $"{nameof(GridDetectorComponent)} processor init".ToLog();
        }
        protected override void OnCreated(in InstanceID entity, ref GridDetectorComponent component)
        {
            "GridDetectorComponent proces creat".ToLog();
        }
        protected override void OnDestroy(in InstanceID entity, ref GridDetectorComponent component)
        {
            "GridDetectorComponent proces destroy".ToLog();

            PresentationSystem<DefaultPresentationGroup, WorldGridSystem>
                .System.GetModule<GridDetectorModule>()
                .RemoveDetector(in entity, ref component);
            //component.m_ObserveIndices
        }
    }
}
