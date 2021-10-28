#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Presentation.Map
{
    internal unsafe sealed class GridDetectionModule : PresentationSystemModule<GridSystem>
    {
        /// <summary>
        /// Grid 위에 있는 Entity 들을 저장합니다.
        /// </summary>
        /// <remarks>
        /// 원본은 <seealso cref="GridSystem.m_GridEntities"/> 이므로 이 모듈에서 상호작용하는 것은 적합하지 않습니다.
        /// </remarks>
        private UnsafeMultiHashMap<int, EntityID> m_GridEntities;
        /// <summary>
        /// Grid 를 감시하는 Observer 들의 그리드 인덱스들을 저장합니다.
        /// </summary>
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

        /// <summary>
        /// 해당 그리드 인덱스가 Observer에 의해 감시되고 있는지를 반환하고, 감시하는 Observer 들을 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsObserveIndex(in int index)
        {
            return m_GridObservers.ContainsKey(index);
        }
        /// <summary><inheritdoc cref="IsObserveIndex(in int)"/></summary>
        /// <param name="index"></param>
        /// <param name="observers"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Observer 로 지정된 Entity 를 업데이트합니다.
        /// </summary>
        /// <remarks>
        /// 대개 <seealso cref="GridDetectorAttribute"/> 를 상속받는 모든 Entity 들은 전부 Observer 가 됩니다.
        /// </remarks>
        /// <param name="entity"></param>
        /// <param name="gridSize"></param>
        /// <param name="postEvent"></param>
        public void UpdateGridDetection(Entity<IEntity> entity, in GridSizeComponent gridSize, bool postEvent)
        {
            ref GridDetectorComponent detector = ref entity.GetComponent<GridDetectorComponent>();
            // 새로운 그리드 Observation 을 위해 이 Entity 의 기존 Observe 그리드 인덱스를 제거합니다.
            ClearDetectorObserveIndices(ref m_GridObservers, entity.Idx, ref detector);

            int maxCount = detector.MaxDetectionIndicesCount;

            int* buffer = stackalloc int[maxCount];
            System.GetRange(in buffer, in maxCount, gridSize.positions[0].index, detector.m_MaxDetectionRange, detector.m_IgnoreLayers, out int count);

            FixedList512Bytes<EntityShortID> newDetected = new FixedList512Bytes<EntityShortID>();

            for (int i = 0; i < count; i++)
            {
                m_GridObservers.Add(buffer[i], entity.Idx);
                detector.m_ObserveIndices.Add(buffer[i]);

                Detection(entity, ref detector, in buffer[i], ref newDetected, postEvent);
            }

            // 이 곳은 이전에 발견했으나, 이제는 조건이 달라져 발견하지 못한 Entity 들을 처리합니다.
            for (int i = 0; i < detector.m_Detected.Length; i++)
            {
                if (newDetected.Contains(detector.m_Detected[i]))
                {
                    //"already detect".ToLog();
                    continue;
                }
                else if (detector.m_DetectRemoveCondition.Execute(entity.As<IEntity, IEntityData>(), out bool predicate) && predicate)
                {
                    continue;
                }

                EntityID targetID = detector.m_Detected[i].GetEntityID();
                Entity<IEntity> target = targetID.GetEntity<IEntity>();

                // 만약 이전 타겟이 GridDetectorAttribute 를 상속받고있으면 내가 발견을 이제 못하게 됬음을 알립니다.
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
                RemoveTargetedEntity(ref m_TargetedEntities, in targetID, entity.Idx);

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

        /// <summary>
        /// <see cref="GridSizeAttribute"/> 를 상속받는 모든 Entity 들을 Detector 에 의해 발견되었는 가를 연산합니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="gridSize"></param>
        /// <param name="postEvent"></param>
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
                var detectorEntity = detectorID.GetEntityData<IEntityData>();
                ref var detector = ref detectorEntity.GetComponent<GridDetectorComponent>();
                if (detector.m_DetectRemoveCondition.Execute(detectorEntity, out bool predicate) && predicate)
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
                RemoveTargetedEntity(ref m_TargetedEntities, entity.Idx, unDetectors[i]);
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

        #region Static Functions

        private static void RemoveTargetedEntity(ref UnsafeMultiHashMap<EntityID, EntityID> hashMap, in EntityID targetID, in EntityID detectorID)
        {
            if (hashMap.CountValuesForKey(targetID) == 1)
            {
                hashMap.Remove(targetID);
            }
            else
            {
                hashMap.Remove(targetID, detectorID);
            }
        }
        private static bool IsDetectorTriggerable(in GridDetectorComponent detector, Entity<IEntity> target)
        {
            if (detector.m_TriggerOnly.Length == 0) return true;

            for (int i = 0; i < detector.m_TriggerOnly.Length; i++)
            {
                Hash temp = detector.m_TriggerOnly[i].Hash;

                if (target.Hash.Equals(temp))
                {
                    return !detector.m_TriggerOnlyInverse;
                }
            }
            return false;
        }
        private static void ClearDetectorObserveIndices(ref UnsafeMultiHashMap<int, EntityID> hashMap, in EntityID entityID, ref GridDetectorComponent detector)
        {
            for (int i = 0; i < detector.m_ObserveIndices.Length; i++)
            {
                RemoveValueAtHashMap(ref hashMap, detector.m_ObserveIndices[i], in entityID);
            }
            detector.m_ObserveIndices.Clear();
        }
        private static void RemoveValueAtHashMap<T, TA>(ref UnsafeMultiHashMap<T, TA> hashMap, in T key, in TA value)
            where T : unmanaged, IEquatable<T>
            where TA : unmanaged, IEquatable<TA>
        {
            if (hashMap.CountValuesForKey(key) == 1)
            {
                hashMap.Remove(key);
            }
            else
            {
                hashMap.Remove(key, value);
            }
        }

        #endregion
    }
}
