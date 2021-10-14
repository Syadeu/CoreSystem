using Syadeu.Collections;
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
        internal FixedList128Bytes<int> m_IgnoreLayers;
        internal ReferenceArray<Reference<EntityBase>> m_TriggerOnly;
        internal bool m_TriggerOnlyInverse;

        // i found
        internal FixedList512Bytes<EntityShortID> m_Detected;
        // ive spotted
        internal FixedList512Bytes<EntityShortID> m_TargetedBy;

        public int MaxDetectionIndicesCount
        {
            get
            {
                int height = ((m_MaxDetectionRange * 2) + 1);
                return height * height;
            }
        }

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

            if (m_TriggerOnly.IsValid())
            {
                m_TriggerOnly.Dispose();
            }

            m_ObserveIndices.Clear();
            m_IgnoreLayers.Clear();
            m_Detected.Clear();
            m_TargetedBy.Clear();
        }

        public bool IsObserveIndex(in int gridIndex)
        {
            return m_ObserveIndices.Contains(gridIndex);
        }

        //internal void RemoveDetected(in int gridIndex)
        //{
        //    for (int i = m_Detected.Length - 1; i >= 0; i--)
        //    {
        //        var temp = m_Detected[i].GetEntity<IEntity>().GetComponent<GridSizeComponent>();
        //        if (temp.IsMyIndex(gridIndex))
        //        {
        //            m_Detected.RemoveAt(i);
        //            continue;
        //        }
        //    }
        //}
    }
}
