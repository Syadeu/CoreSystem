#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation.Map
{
    internal unsafe sealed class GridDetectionModule : PresentationSystemModule<GridSystem>
    {
        private UnsafeMultiHashMap<int, EntityID> m_GridEntities;
        private UnsafeMultiHashMap<int, EntityID> m_GridObservers;

        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            m_GridObservers = new UnsafeMultiHashMap<int, EntityID>(1024, AllocatorManager.Persistent);

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_GridObservers.Dispose();

            m_EventSystem = null;
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;
        }

        #endregion

        public void UpdateHashMap(UnsafeMultiHashMap<int, EntityID> gridEntities, int length)
        {
            m_GridEntities = gridEntities;
            if (m_GridObservers.IsCreated)
            {
                m_GridObservers.Dispose();
            }
            m_GridObservers = new UnsafeMultiHashMap<int, EntityID>(length, AllocatorManager.Persistent);
        }
        public void ClearHashMap()
        {
            m_GridObservers.Clear();
        }

        public bool IsObserveIndex(in int index)
        {
            return m_GridObservers.ContainsKey(index);
        }
        public bool IsObserveIndex(in int index, out UnsafeMultiHashMap<int, EntityID>.Enumerator observers)
        {
            if (!IsObserveIndex(in index))
            {
                observers = default(UnsafeMultiHashMap<int, EntityID>.Enumerator);
                return false;
            }

            observers = m_GridObservers.GetValuesForKey(index);
            return true;
        }

        public void UpdateGridDetection(Entity<IEntity> entity, in GridSizeComponent gridSize)
        {
            ref GridDetectorComponent detector = ref entity.GetComponent<GridDetectorComponent>();
            for (int i = 0; i < detector.m_ObserveIndices.Length; i++)
            {
                if (m_GridObservers.CountValuesForKey(detector.m_ObserveIndices[i]) == 1)
                {
                    m_GridObservers.Remove(detector.m_ObserveIndices[i]);
                }
                else
                {
                    m_GridObservers.Remove(detector.m_ObserveIndices[i], entity.Idx);
                }
            }
            detector.m_ObserveIndices.Clear();

            int maxCount = detector.MaxDetectionIndicesCount;

            int* buffer = stackalloc int[maxCount];
            System.GetRange(in buffer, in maxCount, gridSize.positions[0].index, detector.m_MaxDetectionRange, detector.m_IgnoreLayers, out int count);

            FixedList512Bytes<EntityShortID>
                    newDetected = new FixedList512Bytes<EntityShortID>();
            //GridDetectorAttribute detectorAtt = entity.GetAttribute<GridDetectorAttribute>();

            for (int i = 0; i < count; i++)
            {
                m_GridObservers.Add(buffer[i], entity.Idx);
                detector.m_ObserveIndices.Add(buffer[i]);

                Detection(entity, ref detector, in buffer[i], ref newDetected);
            }

            for (int i = 0; i < detector.m_Detected.Length; i++)
            {
                if (newDetected.Contains(detector.m_Detected[i])) continue;

                Entity<IEntity> target = detector.m_Detected[i].GetEntityID().GetEntity<IEntity>();

                if (target.HasComponent<GridDetectorComponent>())
                {
                    ref var targetDetector = ref target.GetComponent<GridDetectorComponent>();
                    EntityShortID myShortID = entity.Idx.GetShortID();

                    if (targetDetector.m_TargetedBy.Contains(myShortID))
                    {
                        targetDetector.m_TargetedBy.Remove(myShortID);
                    }
                }

                m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(entity, target, false));
            }

            detector.m_Detected = newDetected;
        }
        private void Detection(Entity<IEntity> entity, ref GridDetectorComponent detector, in int index, ref FixedList512Bytes<EntityShortID> newDetected)
        {
            if (!m_GridEntities.TryGetFirstValue(index, out EntityID targetID, out var iter))
            {
                return;
            }

            do
            {
                Entity<IEntity> target = targetID.GetEntity<IEntity>();
                if (targetID.Equals(entity.Idx) || !IsDetectorTriggerable(in detector, target)) continue;

                EntityShortID targetShortID = targetID.GetShortID();

                if (detector.m_Detected.Contains(targetShortID))
                {
                    newDetected.Add(targetShortID);
                    continue;
                }

                EntityData<IEntityData>
                    myDat = entity.As<IEntity, IEntityData>(),
                    targetDat = target.As<IEntity, IEntityData>();

                detector.m_OnDetectedPredicate.Execute(myDat, out bool predicate);
                if (!predicate)
                {
                    "predicate failed".ToLog();
                    continue;
                }

                //detector.m_Detected.Add(targetShortID);
                newDetected.Add(targetShortID);
                m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(entity, target, true));

                for (int i = 0; i < detector.m_OnDetected.Length; i++)
                {
                    detector.m_OnDetected[i].Execute(myDat, targetDat);
                }

                //"detect".ToLog();
                if (target.HasComponent<GridDetectorComponent>())
                {
                    ref var targetDetector = ref target.GetComponent<GridDetectorComponent>();
                    EntityShortID myShortID = entity.Idx.GetShortID();

                    if (!targetDetector.m_TargetedBy.Contains(myShortID))
                    {
                        targetDetector.m_TargetedBy.Add(myShortID);
                    }
                }

            } while (m_GridEntities.TryGetNextValue(out targetID, ref iter));
        }

        private static bool IsDetectorTriggerable(in GridDetectorComponent detector, Entity<IEntity> target)
        {
            if (detector.m_TriggerOnly.Length == 0) return true;

            for (int i = 0; i < detector.m_TriggerOnly.Length; i++)
            {
                Hash temp = detector.m_TriggerOnly[i].Hash;

                if (target.Hash.Equals(temp))
                {
                    return detector.m_TriggerOnlyInverse;
                }
            }
            return false;
        }
    }
}
