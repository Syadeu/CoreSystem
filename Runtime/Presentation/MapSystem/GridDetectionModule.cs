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

        // 1. targeted, 2. spotteds(observers)
        private UnsafeMultiHashMap<EntityID, EntityID> m_TargetedEntities;

        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            m_GridObservers = new UnsafeMultiHashMap<int, EntityID>(1024, AllocatorManager.Persistent);
            m_TargetedEntities = new UnsafeMultiHashMap<EntityID, EntityID>(1024, AllocatorManager.Persistent);

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_GridObservers.Dispose();
            m_TargetedEntities.Dispose();

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

        public void UpdateGridDetection(Entity<IEntity> entity, in GridSizeComponent gridSize, bool postEvent)
        {
            //"in".ToLog();
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

                Detection(entity, ref detector, in buffer[i], ref newDetected, postEvent);
            }

            for (int i = 0; i < detector.m_Detected.Length; i++)
            {
                if (newDetected.Contains(detector.m_Detected[i]))
                {
                    //"already detect".ToLog();
                    continue;
                }

                EntityID targetID = detector.m_Detected[i].GetEntityID();
                Entity<IEntity> target = targetID.GetEntity<IEntity>();

                if (target.HasComponent<GridDetectorComponent>())
                {
                    ref var targetDetector = ref target.GetComponent<GridDetectorComponent>();
                    EntityShortID myShortID = entity.Idx.GetShortID();

                    if (targetDetector.m_TargetedBy.Contains(myShortID))
                    {
                        targetDetector.m_TargetedBy.Remove(myShortID);
                    }
                }

                //"un detect".ToLog();
                if (m_TargetedEntities.CountValuesForKey(targetID) == 1)
                {
                    m_TargetedEntities.Remove(targetID);
                }
                else
                {
                    m_TargetedEntities.Remove(targetID, entity.Idx);
                }
                
                if (postEvent)
                {
                    m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(entity, target, false));
                }
            }

            detector.m_Detected = newDetected;
        }
        private void Detection(Entity<IEntity> entity, ref GridDetectorComponent detector, in int index, ref FixedList512Bytes<EntityShortID> newDetected, bool postEvent)
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

                if (postEvent)
                {
                    m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(entity, target, true));
                }
                
                m_TargetedEntities.Add(targetID, entity.Idx);

                for (int i = 0; i < detector.m_OnDetected.Length; i++)
                {
                    detector.m_OnDetected[i].Execute(myDat, targetDat);
                }

                //$"1. detect {entity.Name} spot {target.Name}".ToLog();
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

        public void UpdateDetectPosition(Entity<IEntity> entity, in GridSizeComponent gridSize, bool postEvent)
        {
            //$"{entity.Name} check observers".ToLog();

            FixedList512Bytes<EntityID>
                    detectors = new FixedList512Bytes<EntityID>();

            for (int i = 0; i < gridSize.positions.Length; i++)
            {
                CheckObservers(entity, gridSize.positions[i].index, ref detectors, postEvent);
            }

            bool entityHasDetector = entity.HasComponent<GridDetectorComponent>();

            //$"1. detect count {detectors.Length} : {m_TargetedEntities.CountValuesForKey(entity.Idx)}".ToLog();

            FixedList512Bytes<EntityID>
                    unDetectors = new FixedList512Bytes<EntityID>();
            EntityShortID myShortID = entity.Idx.GetShortID();

            foreach (EntityID detectorID in m_TargetedEntities.GetValuesForKey(entity.Idx))
            {
                if (detectors.Contains(detectorID))
                {
                    continue;
                }

                if (entityHasDetector)
                {
                    ref var myDetector = ref entity.GetComponent<GridDetectorComponent>();
                    EntityShortID detectorShortID = detectorID.GetShortID();

                    if (myDetector.m_TargetedBy.Contains(detectorShortID))
                    {
                        myDetector.m_TargetedBy.Remove(detectorShortID);
                    }
                }

                ref var detector = ref detectorID.GetEntity<IEntity>().GetComponent<GridDetectorComponent>();
                detector.m_Detected.Remove(myShortID);

                if (postEvent)
                {
                    m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(detectorID.GetEntity<IEntity>(), entity, false));
                }

                unDetectors.Add(detectorID);
            }

            //$"2. undetect count {unDetectors.Length}".ToLog();

            for (int i = 0; i < unDetectors.Length; i++)
            {
                if (m_TargetedEntities.CountValuesForKey(entity.Idx) == 1)
                {
                    m_TargetedEntities.Remove(entity.Idx);
                }
                else
                {
                    m_TargetedEntities.Remove(entity.Idx, unDetectors[i]);
                }
            }
        }
        private void CheckObservers(Entity<IEntity> entity, in int index, ref FixedList512Bytes<EntityID> detectors, bool postEvent)
        {
            if (!m_GridObservers.TryGetFirstValue(index, out EntityID observerID, out var iter))
            {
                return;
            }

            EntityShortID targetShortID = entity.Idx.GetShortID();
            EntityData<IEntityData> targetData = entity.As<IEntity, IEntityData>();
            do
            {
                EntityData<IEntityData> observer = observerID.GetEntityData<IEntityData>();
                ref GridDetectorComponent detector = ref observer.GetComponent<GridDetectorComponent>();

                if (observerID.Equals(entity.Idx) || !IsDetectorTriggerable(in detector, entity)) continue;

                //$"detector search {observer.Name} ".ToLog();

                if (detector.m_Detected.Contains(targetShortID))
                {
                    //$"detector {observer.Name} already have {entity.Name}".ToLog();
                    detectors.Add(observerID);
                    continue;
                }

                detector.m_OnDetectedPredicate.Execute(targetData, out bool predicate);
                if (!predicate)
                {
                    "predicate failed".ToLog();
                    continue;
                }

                detector.m_Detected.Add(targetShortID);
                detectors.Add(observerID);

                if (postEvent)
                {
                    m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(observerID.GetEntity<IEntity>(), entity, true));
                }

                m_TargetedEntities.Add(entity.Idx, observerID);

                for (int i = 0; i < detector.m_OnDetected.Length; i++)
                {
                    detector.m_OnDetected[i].Execute(observer, targetData);
                }

                //$"2. detect {observer.Name} spot {entity.Name}".ToLog();
                if (targetData.HasComponent<GridDetectorComponent>())
                {
                    ref var targetDetector = ref targetData.GetComponent<GridDetectorComponent>();

                    if (!targetDetector.m_TargetedBy.Contains(targetShortID))
                    {
                        targetDetector.m_TargetedBy.Add(targetShortID);
                    }
                }

            } while (m_GridObservers.TryGetNextValue(out observerID, ref iter));
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
