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
using Syadeu.Presentation.Events;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Syadeu.Presentation.Grid
{
    internal unsafe sealed class GridDetectorModule : PresentationSystemModule<WorldGridSystem>
    {
        /// <summary>
        /// Grid 를 감시하는 Observer 들의 그리드 인덱스들을 저장합니다.
        /// </summary>
        private NativeMultiHashMap<GridIndex, InstanceID> m_GridObservers;
        //private NativeHashSet<GridIndex> m_ObservedIndices;
        // 1. targeted, 2. spotteds(observers)
        private NativeMultiHashMap<InstanceID, InstanceID> m_TargetedEntities;

        private Unity.Profiling.ProfilerMarker
            m_UpdateGridDetectionMarker = new Unity.Profiling.ProfilerMarker($"{nameof(GridDetectorModule)}.UpdateGridDetection"),
            m_ClearExistGridDetectionMarker = new Unity.Profiling.ProfilerMarker($"{nameof(GridDetectorModule)}.ClearExistGridDetection"),
            m_UpdateExistGridDetectionMarker = new Unity.Profiling.ProfilerMarker($"{nameof(GridDetectorModule)}.UpdateExistGridDetection"),
            m_UpdateRemoveGridDetectionMarker = new Unity.Profiling.ProfilerMarker($"{nameof(GridDetectorModule)}.UpdateRemoveGridDetection"),

            m_UpdateDetectPositionMarker = new Unity.Profiling.ProfilerMarker($"{nameof(GridDetectorModule)}.UpdateDetectPosition");

        private EventSystem m_EventSystem;

        public NativeMultiHashMap<GridIndex, InstanceID> GridObservers => m_GridObservers;
        public NativeMultiHashMap<InstanceID, InstanceID> TargetedEntities => m_TargetedEntities;

        #region Presentation Methods

        protected override void OnInitialize()
        {
            m_GridObservers = new NativeMultiHashMap<GridIndex, InstanceID>(1024, AllocatorManager.Persistent);
            m_TargetedEntities = new NativeMultiHashMap<InstanceID, InstanceID>(1024, AllocatorManager.Persistent);

            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
        }
        protected override void OnShutDown()
        {
            m_EventSystem.RemoveEvent<OnGridLocationChangedEvent>(OnGridLocationChangedEventHandler);

            //m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;
        }
        protected override void OnDispose()
        {
            m_GridObservers.Dispose();
            m_TargetedEntities.Dispose();

            m_EventSystem = null;
            //m_EntitySystem = null;
        }

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnGridLocationChangedEvent>(OnGridLocationChangedEventHandler);
        }

        private void OnGridLocationChangedEventHandler(OnGridLocationChangedEvent ev)
        {
            if (!ev.Entity.HasComponent<GridComponent>()) return;

            System.CompleteGridJob();

            if (ev.Entity.HasComponent<GridDetectorComponent>())
            {
                UpdateGridDetection(ev.Entity, ev.Entity.GetComponent<GridComponent>(), true);
            }

            UpdateDetectPosition(ev.Entity, ev.Entity.GetComponent<GridComponent>(), true);
        }

        #endregion

        public void Remove(in InstanceID entity)
        {
            if (!m_TargetedEntities.TryGetFirstValue(entity, out InstanceID observerID, out var iter)) return;

            do
            {
#if DEBUG_MODE
                if (!observerID.HasComponent<GridDetectorComponent>())
                {
                    CoreSystem.Logger.LogError(LogChannel.Presentation,
                        $"grid err");
                    continue;
                }
#endif
                ref GridDetectorComponent observer = ref observerID.GetComponent<GridDetectorComponent>();

                observer.m_Detected.Remove(entity);
            } while (m_TargetedEntities.TryGetNextValue(out observerID, ref iter));

            m_TargetedEntities.Remove(entity);
        }
        public void RemoveDetector(in InstanceID entity, ref GridDetectorComponent detector)
        {
            ClearDetectorObserveIndices(ref m_GridObservers, entity, ref detector);
        }
        
        /// <summary>
        /// <paramref name="entity"/> 가 <paramref name="index"/> 를 감시하는지 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool IsObserveIndexOf(in GridIndex index, in InstanceID entity)
        {
            if (m_GridObservers.TryGetFirstValue(index, out InstanceID temp, out var iter))
            {
                do
                {
                    if (temp.Equals(entity)) return true;

                } while (m_GridObservers.TryGetNextValue(out temp, ref iter));
            }

            return false;
        }
        /// <summary>
        /// <paramref name="entity"/> 만 <paramref name="index"/> 를 감시하는지 반환합니다. 
        /// 다른 Entity 가 해당 그리드 좌표를 감시하면 <see langword="false"/> 를 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool IsObserveIndexOfOnly(in GridIndex index, in InstanceID entity)
        {
            if (m_GridObservers.TryGetFirstValue(index, out InstanceID temp, out _) &&
                m_GridObservers.CountValuesForKey(index) == 1)
            {
                if (temp.Equals(entity)) return true;
            }

            return false;
        }

        /// <summary>
        /// <paramref name="index"/> 가 Observer에 의해 감시되고 있는지 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool IsObserveIndex(in GridIndex index)
        {
            return m_GridObservers.ContainsKey(index);
        }
        /// <summary>
        /// <paramref name="index"/> 가 Observer에 의해 감시되고 있는지를 반환하고, 감시하는 Observer 들을 반환합니다.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="observers"></param>
        /// <returns></returns>
        public bool IsObserveIndex(in GridIndex index, out NativeMultiHashMap<GridIndex, InstanceID>.Enumerator observers)
        {
            if (!IsObserveIndex(in index))
            {
                observers = default(NativeMultiHashMap<GridIndex, InstanceID>.Enumerator);
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
        public void UpdateGridDetection(Entity<IEntity> entity, in GridComponent gridSize, bool postEvent)
        {
            using (m_UpdateGridDetectionMarker.Auto())
            {
                ref GridDetectorComponent detector = ref entity.GetComponent<GridDetectorComponent>();

                // 새로운 그리드 Observation 을 위해 이 Entity 의 기존 Observe 그리드 인덱스를 제거합니다.
                using (m_ClearExistGridDetectionMarker.Auto())
                {
                    ClearDetectorObserveIndices(ref m_GridObservers, entity.Idx, ref detector);
                }

                //int maxCount = detector.MaxDetectionIndicesCount;

                FixedList512Bytes<InstanceID> newDetected = new FixedList512Bytes<InstanceID>();

                // 임시로 같은 층에 있는 엔티티만 감시함
                using (m_UpdateExistGridDetectionMarker.Auto())
                {
                    foreach (var item in System.GetRange(entity.Idx, new int3(detector.DetectedRange, 0, detector.DetectedRange)))
                    {
                        m_GridObservers.Add(item, entity.Idx);
                        detector.m_ObserveIndices.Add(item);

                        Detection(entity, ref detector, item, ref newDetected, postEvent);
                    }
                }
                
                using (m_UpdateRemoveGridDetectionMarker.Auto())
                {
                    // 이 곳은 이전에 발견했으나, 이제는 조건이 달라져 발견하지 못한 Entity 들을 처리합니다.
                    for (int i = 0; i < detector.m_Detected.Length; i++)
                    {
                        if (newDetected.Contains(detector.m_Detected[i]))
                        {
                            //"already detect".ToLog();
                            continue;
                        }
                        else if (detector.m_DetectRemoveCondition.Execute(entity.ToEntity<IObject>(), out bool predicate) && predicate)
                        {
                            continue;
                        }

                        InstanceID targetID = detector.m_Detected[i];
                        Entity<IEntity> target = targetID.GetEntity<IEntity>();

                        // 만약 이전 타겟이 GridDetectorAttribute 를 상속받고있으면 내가 발견을 이제 못하게 됬음을 알립니다.
                        if (target.HasComponent<GridDetectorComponent>())
                        {
                            ref var targetDetector = ref target.GetComponent<GridDetectorComponent>();

                            if (targetDetector.m_TargetedBy.Contains(entity.Idx))
                            {
                                targetDetector.m_TargetedBy.Remove(entity.Idx);
                            }
                        }

                        //"un detect".ToLog();
                        RemoveTargetedEntity(ref m_TargetedEntities, in targetID, entity.Idx);

                        if (postEvent)
                        {
                            m_EventSystem.PostEvent(OnGridDetectEntityEvent.GetEvent(entity, target, false));
                        }
                    }
                }
                
                detector.m_Detected = newDetected;
            }
        }
        private void Detection(Entity<IEntity> entity, ref GridDetectorComponent detector, in GridIndex index, ref FixedList512Bytes<InstanceID> newDetected, bool postEvent)
        {
            if (!System.m_Indices.TryGetFirstValue(index.Index, out InstanceID targetID, out var iter))
            {
                return;
            }

            do
            {
                Entity<IEntity> target = targetID.GetEntity<IEntity>();
                if (targetID.Equals(entity.Idx) || !IsDetectorTriggerable(in detector, target)) continue;

                //EntityShortID targetShortID = targetID.GetShortID();

                if (detector.m_Detected.Contains(targetID))
                {
                    newDetected.Add(targetID);
                    continue;
                }

                Entity<IObject>
                    myDat = entity.ToEntity<IObject>(),
                    targetDat = target.ToEntity<IObject>();

                detector.m_OnDetectedPredicate.Execute(myDat, out bool predicate);
                if (!predicate)
                {
                    "predicate failed".ToLog();
                    continue;
                }

                //detector.m_Detected.Add(targetShortID);
                newDetected.Add(targetID);

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
                    //EntityShortID myShortID = entity.Idx.GetShortID();

                    if (!targetDetector.m_TargetedBy.Contains(entity.Idx))
                    {
                        targetDetector.m_TargetedBy.Add(entity.Idx);
                    }
                }

            } while (System.m_Indices.TryGetNextValue(out targetID, ref iter));
        }

        /// <summary>
        /// <see cref="GridComponent"/> 를 상속받는 모든 Entity 들을 Detector 에 의해 발견되었는가를 연산합니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="gridSize"></param>
        /// <param name="postEvent"></param>
        public void UpdateDetectPosition(Entity<IEntity> entity, in GridComponent gridSize, bool postEvent)
        {
            using (m_UpdateDetectPositionMarker.Auto())
            {
                FixedList512Bytes<InstanceID>
                    detectors = new FixedList512Bytes<InstanceID>();

                for (int i = 0; i < gridSize.Indices.Length; i++)
                {
                    CheckObservers(entity, gridSize.Indices[i], ref detectors, postEvent);
                }

                bool entityHasDetector = entity.HasComponent<GridDetectorComponent>();

                //$"1. detect count {detectors.Length} : {m_TargetedEntities.CountValuesForKey(entity.Idx)}".ToLog();

                FixedList512Bytes<InstanceID>
                        unDetectors = new FixedList512Bytes<InstanceID>();
                InstanceID myShortID = entity.Idx;

                foreach (InstanceID detectorID in m_TargetedEntities.GetValuesForKey(entity.Idx))
                {
                    if (detectors.Contains(detectorID))
                    {
                        continue;
                    }
                    var detectorEntity = detectorID.GetEntity<IObject>();
                    ref var detector = ref detectorEntity.GetComponent<GridDetectorComponent>();
                    if (detector.m_DetectRemoveCondition.Execute(detectorEntity, out bool predicate) && predicate)
                    {
                        continue;
                    }

                    if (entityHasDetector)
                    {
                        ref var myDetector = ref entity.GetComponent<GridDetectorComponent>();
                        //EntityShortID detectorShortID = detectorID;

                        if (myDetector.m_TargetedBy.Contains(detectorID))
                        {
                            myDetector.m_TargetedBy.Remove(detectorID);
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
        }
        private void CheckObservers(Entity<IEntity> entity, in GridIndex index, ref FixedList512Bytes<InstanceID> detectors, bool postEvent)
        {
            if (!m_GridObservers.TryGetFirstValue(index, out InstanceID observerID, out var iter))
            {
                return;
            }

            InstanceID targetShortID = entity.Idx;
            Entity<IObject> targetData = entity.ToEntity<IObject>();
            do
            {
                //observerID.IsEntity

                Entity<IObject> observer = observerID.GetEntity<IObject>();
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

        private static void RemoveTargetedEntity(ref NativeMultiHashMap<InstanceID, InstanceID> hashMap, in InstanceID targetID, in InstanceID detectorID)
        {
            //if (hashMap.CountValuesForKey(targetID) == 1)
            //{
            //    hashMap.Remove(targetID);
            //}
            //else
            //{
            //    hashMap.Remove(targetID, detectorID);
            //}

            RemoveValueAtHashMap(ref hashMap, in targetID, in detectorID);
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
        private static void ClearDetectorObserveIndices(ref NativeMultiHashMap<GridIndex, InstanceID> hashMap, in InstanceID entityID, ref GridDetectorComponent detector)
        {
            for (int i = 0; i < detector.m_ObserveIndices.Length; i++)
            {
                RemoveValueAtHashMap(ref hashMap, detector.m_ObserveIndices[i], in entityID);
            }
            detector.m_ObserveIndices.Clear();
        }
        private static bool RemoveValueAtHashMap<T, TA>(ref NativeMultiHashMap<T, TA> hashMap, in T key, in TA value)
            where T : unmanaged, IEquatable<T>
            where TA : unmanaged, IEquatable<TA>
        {
            if (hashMap.CountValuesForKey(key) == 1)
            {
                hashMap.Remove(key);
                return true;
            }

            hashMap.Remove(key, value);
            return false;
        }

        #endregion
    }
}
