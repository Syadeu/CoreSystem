using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using System;
using System.Linq;
using Unity.Collections;

namespace Syadeu.Presentation.Map
{
    public struct GridDetectorComponent : IEntityComponent, IDisposable
    {
        internal EntityShortID m_MyShortID;
        internal int m_MaxDetectionRange;
        internal FixedList4096Bytes<int> m_ObserveIndices;
        internal GridLayerChain m_IgnoreLayers;
        
        internal FixedReferenceList64<EntityBase> m_TriggerOnly;
        internal bool m_TriggerOnlyInverse;

        internal FixedReferenceList64<TriggerPredicateAction> m_OnDetectedPredicate;
        internal FixedLogicTriggerAction8 m_OnDetected;

        // i found
        internal FixedList512Bytes<EntityShortID> m_Detected;
        // ive spotted
        internal FixedList512Bytes<EntityShortID> m_TargetedBy;

        public FixedList512Bytes<EntityShortID> Detected => m_Detected;
        public FixedList512Bytes<EntityShortID> TargetedBy => m_TargetedBy;

        public int MaxDetectionIndicesCount => GridSizeComponent.CalculateMaxiumIndicesInRangeCount(m_MaxDetectionRange);

        void IDisposable.Dispose()
        {
            for (int i = 0; i < m_Detected.Length; i++)
            {
                var target = m_Detected[i].GetEntityID().GetEntity<IEntity>();
                if (!target.HasComponent<GridDetectorComponent>())
                {
                    continue;
                }

                ref var targetDetector = ref target.GetComponent<GridDetectorComponent>();
                targetDetector.m_TargetedBy.Remove(m_MyShortID);
            }
            for (int i = 0; i < m_TargetedBy.Length; i++)
            {
                var target = m_TargetedBy[i].GetEntityID().GetEntity<IEntity>();
                ref var targetDetector = ref target.GetComponent<GridDetectorComponent>();

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
