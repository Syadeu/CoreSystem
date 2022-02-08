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
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Collections.Threading;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Grid.LowLevel;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Grid
{
    public sealed class WorldGridSystem : PresentationSystemEntity<WorldGridSystem>,
        INotifySystemModule<WorldGridPathModule>,
        INotifySystemModule<GridDetectorModule>
#if CORESYSTEM_SHAPES
        , INotifySystemModule<WorldGridShapesModule>
#endif
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private WorldGrid m_Grid;
        private JobHandle m_GridUpdateJob;

        internal NativeMultiHashMap<ulong, InstanceID> m_Indices;
        private NativeMultiHashMap<InstanceID, ulong> m_Entities;
        private NativeQueue<InstanceID>
            m_WaitForAdd, m_WaitForRemove;

        private UnsafeFixedQueue<InstanceID> m_NeedUpdateEntities;

        private bool m_EnabledCursorObserve = false;

        private EntityComponentSystem m_ComponentSystem;
        private EventSystem m_EventSystem;
        private InputSystem m_InputSystem;
        private LevelDesignSystem m_LevelDesignSystem;

        internal WorldGrid Grid => m_Grid;
        public float CellSize => m_Grid.cellSize;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            m_Grid = new WorldGrid(new AABB(0, 0), 2.5f);

            m_WaitForAdd = new NativeQueue<InstanceID>(AllocatorManager.Persistent);
            m_WaitForRemove = new NativeQueue<InstanceID>(AllocatorManager.Persistent);

            m_Indices = new NativeMultiHashMap<ulong, InstanceID>(1024, AllocatorManager.Persistent);
            m_Entities = new NativeMultiHashMap<InstanceID, ulong>(1024, AllocatorManager.Persistent);

            m_NeedUpdateEntities = new UnsafeFixedQueue<InstanceID>(128, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            RequestSystem<DefaultPresentationGroup, EntityComponentSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);

            RequestSystem<LevelDesignPresentationGroup, LevelDesignSystem>(Bind);

            return base.OnInitialize();
        }
        protected override void OnShutDown()
        {
            m_ComponentSystem.OnComponentAdded -= M_ComponentSystem_OnComponentAdded;
            m_ComponentSystem.OnComponentRemove -= M_ComponentSystem_OnComponentRemove;

            m_EventSystem.RemoveEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }
        protected override void OnDispose()
        {
            m_WaitForAdd.Dispose();
            m_WaitForRemove.Dispose();

            m_Indices.Dispose();
            m_Entities.Dispose();

            m_NeedUpdateEntities.Dispose();

            m_ComponentSystem = null;
            m_EventSystem = null;
            m_InputSystem = null;

            m_LevelDesignSystem = null;
        }

        #region Binds

        private void Bind(EntityComponentSystem other)
        {
            m_ComponentSystem = other;

            m_ComponentSystem.OnComponentAdded += M_ComponentSystem_OnComponentAdded;
            m_ComponentSystem.OnComponentRemove += M_ComponentSystem_OnComponentRemove;
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }
        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }
        private void Bind(LevelDesignSystem other)
        {
            m_LevelDesignSystem = other;
        }

        private void M_ComponentSystem_OnComponentAdded(InstanceID arg1, Type arg2)
        {
            if (!TypeHelper.TypeOf<GridComponent>.Type.IsAssignableFrom(arg2)) return;

            m_WaitForAdd.Enqueue(arg1);
        }
        private void M_ComponentSystem_OnComponentRemove(InstanceID arg1, Type arg2)
        {
            if (!TypeHelper.TypeOf<GridComponent>.Type.IsAssignableFrom(arg2)) return;

            m_GridUpdateJob.Complete();

            m_WaitForRemove.Enqueue(arg1);
        }

        #endregion

        #region Event Handlers

        private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        {
            if (ev.entity.HasComponent<GridComponent>())
            {
                m_NeedUpdateEntities.Enqueue(ev.entity.Idx);
            }

        }

        #endregion

        protected override PresentationResult BeforePresentation()
        {
            bool requireReindex = false;

            while (m_NeedUpdateEntities.TryDequeue(out InstanceID entity))
            {
                Remove(entity);
                ref GridComponent component = ref entity.GetComponent<GridComponent>();
                FixedList4096Bytes<GridIndex> prev = component.Indices;
                requireReindex |= !Add(entity, ref component, out bool locationChanged);

                if (locationChanged)
                {
                    m_EventSystem.PostEvent(
                        OnGridLocationChangedEvent.GetEvent(entity, prev, component.Indices, false));
                }
            }

            if (!requireReindex)
            {
                int removeCount = m_WaitForRemove.Count;
                for (int i = 0; i < removeCount; i++)
                {
                    InstanceID entity = m_WaitForRemove.Dequeue();
                    Remove(entity);
                }

                int addCount = m_WaitForAdd.Count;

                for (int i = 0; i < addCount; i++)
                {
                    InstanceID entity = m_WaitForAdd.Dequeue();
                    ref GridComponent component = ref entity.GetComponent<GridComponent>();
                    requireReindex |= !Add(entity, ref component, out bool locationChanged);

                    if (locationChanged)
                    {
                        m_EventSystem.PostEvent(
                            OnGridLocationChangedEvent.GetEvent(
                                entity, new FixedList4096Bytes<GridIndex>(), component.Indices, false));
                    }
                }
            }
            
            if (requireReindex) FullIndexingUpdate();

            UpdateCursorObservation();

            return base.BeforePresentation();
        }

        #endregion

        private void Increase(AABB aabb)
        {
            float3
                min = math.round(aabb.min),
                max = math.round(aabb.max),

                restMin = min % m_Grid.cellSize,
                restMax = max % m_Grid.cellSize,

                targetMin = min + restMin - m_Grid.cellSize,
                targetMax = max - restMax + m_Grid.cellSize;

            AABB temp;
            if (m_Grid.aabb.IsZero())
            {
                temp = new AABB(targetMin, 0);
                temp.Encapsulate(targetMax);
            }
            else
            {
                temp = m_Grid.aabb;

                temp.Encapsulate(targetMin);
                temp.Encapsulate(targetMax);
            }
            
            m_Grid.aabb = temp;
        }
        private bool Add(in InstanceID entity, ref GridComponent component, out bool locationChanged)
        {
            AABB aabb = entity.GetTransformWithoutCheck().aabb;
            locationChanged = false;
            if (!m_Grid.Contains(aabb))
            {
                Increase(aabb);

                //$"require encapsulate for {entity}".ToLog();
                return false;
            }

            //ref GridComponent component = ref entity.GetComponent<GridComponent>();
            if (component.FixedSize.Equals(0))
            {
                var indices = m_Grid.AABBToIndices(aabb);
                if (indices.Length == 0)
                {
                    $"no index found to {entity}".ToLog();
                }

                if (component.m_Indices.Length != indices.Length)
                {
                    locationChanged = true;

                    for (int i = 0; i < indices.Length; i++)
                    {
                        m_Indices.Add(indices[i].Index, entity);
                        m_Entities.Add(entity, indices[i].Index);
                    }
                }
                else
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        m_Indices.Add(indices[i].Index, entity);
                        m_Entities.Add(entity, indices[i].Index);

                        locationChanged |= component.m_Indices[i].Index != indices[i].Index;
                    }
                }
                
                component.m_Indices = indices;
            }
            else
            {
                float3 targetPosition = aabb.lowerCenter;
                if (component.SizeAlignment != Alignment.Center)
                {
                    if ((component.SizeAlignment & Alignment.Left) == Alignment.Left)
                    {
                        targetPosition.x -= aabb.extents.x;
                    }
                    if ((component.SizeAlignment & Alignment.Right) == Alignment.Right)
                    {
                        targetPosition.x += aabb.extents.x;
                    }
                    if ((component.SizeAlignment & Alignment.Down) == Alignment.Down)
                    {
                        targetPosition.z -= aabb.extents.z;
                    }
                    if ((component.SizeAlignment & Alignment.Up) == Alignment.Up)
                    {
                        targetPosition.z += aabb.extents.z;
                    }
                }
                int3 location = m_Grid.PositionToLocation(targetPosition);

                var indices = new FixedList512Bytes<GridIndex>();

                for (int y = 0; y < component.FixedSize.y; y++)
                {
                    for (int x = 0; x < component.FixedSize.x; x++)
                    {
                        for (int z = 0; z < component.FixedSize.z; z++)
                        {
                            ulong index = m_Grid.LocationToIndex(location + new int3(x, y, z));

                            m_Indices.Add(index, entity);
                            m_Entities.Add(entity, index);

                            indices.Add(new GridIndex(m_Grid, index));
                        }
                    }
                }

                if (component.m_Indices.Length != indices.Length)
                {
                    locationChanged = true;
                }
                else
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        locationChanged |= component.m_Indices[i].Index != indices[i].Index;
                    }
                }

                component.m_Indices = indices;
            }

            return true;
        }
        private void Remove(in InstanceID entity)
        {
            if (!m_Entities.TryGetFirstValue(entity, out ulong index, out var iter)) return;

            do
            {
                if (m_Indices.CountValuesForKey(index) == 1)
                {
                    m_Indices.Remove(index);
                }
                else m_Indices.Remove(index, entity);

            } while (m_Entities.TryGetNextValue(out index, ref iter));

            m_Entities.Remove(entity);
        }

        [BurstCompatible]
        private struct CloseDistanceComparer : IComparer<int3>
        {
            private int3 from;

            public CloseDistanceComparer(int3 from)
            {
                this.from = from;
            }

            public int Compare(int3 x, int3 y)
            {
                int3
                    reletiveX = from - x,
                    reletiveY = from - y;

                int
                    sqrX = math.mul(reletiveX, reletiveX),
                    sqrY = math.mul(reletiveY, reletiveY);

                if (sqrX == sqrY) return 0;
                else if (sqrX < sqrY) return -1;
                else return 1;
            }
        }

        #region Jobs

        internal void CompleteGridJob()
        {
            m_GridUpdateJob.Complete();
        }
        private void FullIndexingUpdate()
        {
            m_GridUpdateJob.Complete();
            
            m_WaitForRemove.Clear();
            m_WaitForAdd.Clear();

            int prevCount = m_Entities.Capacity;
            int targetCap = prevCount - m_Entities.Count() < prevCount / 3 ? prevCount * 2 : prevCount;

            if (prevCount != targetCap)
            {
                m_Entities.Dispose(); m_Indices.Dispose();
                m_Indices = new NativeMultiHashMap<ulong, InstanceID>(targetCap, AllocatorManager.Persistent);
                m_Entities = new NativeMultiHashMap<InstanceID, ulong>(targetCap, AllocatorManager.Persistent);
            }
            else
            {
                m_Indices.Clear();
                m_Entities.Clear();
            }

            UpdateGridComponentJob componentJob = new UpdateGridComponentJob(
                m_Grid,
                ref m_Indices,
                ref m_Entities);
            PostChangedEventJob postChangedEventJob = new PostChangedEventJob(
                ref m_Entities,
                m_EventSystem
                );
            NativeMultiHashMap<GridIndex, InstanceID> observers = GetModule<GridDetectorModule>().GridObservers;
            observers.Clear();
            GetModule<GridDetectorModule>().TargetedEntities.Clear();
            FullObserverUpdateJob observerUpdateJob = new FullObserverUpdateJob
            {
                grid = m_Grid,
                entities = m_Entities,
                observers = observers.AsParallelWriter()
            };

            var handle =
                ScheduleAt<UpdateGridComponentJob, GridComponent>(JobPosition.Before, componentJob);
            m_GridUpdateJob = JobHandle.CombineDependencies(m_GridUpdateJob, handle);

            var handle2 =
                ScheduleAt(JobPosition.Before, postChangedEventJob);
            m_GridUpdateJob = JobHandle.CombineDependencies(m_GridUpdateJob, handle2);

            var handle3 = ScheduleAt<FullObserverUpdateJob, GridDetectorComponent>(JobPosition.Before, observerUpdateJob);
            m_GridUpdateJob = JobHandle.CombineDependencies(m_GridUpdateJob, handle3);
            //"schedule full re indexing".ToLog();
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct UpdateGridComponentJob : IJobParallelForEntities<GridComponent>
        {
            [ReadOnly]
            private WorldGrid grid;
            [ReadOnly]
            private EntityTransformHashMap m_TrHashMap;

            [WriteOnly]
            private NativeMultiHashMap<ulong, InstanceID>.ParallelWriter indices;
            [WriteOnly]
            private NativeMultiHashMap<InstanceID, ulong>.ParallelWriter entities;

            public UpdateGridComponentJob(
                WorldGrid grid,
                ref NativeMultiHashMap<ulong, InstanceID> indices,
                ref NativeMultiHashMap<InstanceID, ulong> entities)
            {
                this.grid = grid;
                m_TrHashMap = EntityTransformStatic.GetHashMap();

                this.indices = indices.AsParallelWriter();
                this.entities = entities.AsParallelWriter();
            }

            public void Execute(in InstanceID entity, ref GridComponent component)
            {
                AABB aabb = m_TrHashMap.GetTransform(entity).aabb;

                if (component.FixedSize.Equals(0))
                {
                    component.m_Indices = grid.AABBToIndices(aabb);
                    for (int i = 0; i < component.m_Indices.Length; i++)
                    {
                        indices.Add(component.m_Indices[i].Index, entity);
                        entities.Add(entity, component.m_Indices[i].Index);
                    }

                    return;
                }

                float3 targetPosition = aabb.lowerCenter;
                if (component.SizeAlignment != Alignment.Center)
                { 
                    if ((component.SizeAlignment & Alignment.Left) == Alignment.Left)
                    {
                        targetPosition.x -= aabb.extents.x;
                    }
                    if ((component.SizeAlignment & Alignment.Right) == Alignment.Right)
                    {
                        targetPosition.x += aabb.extents.x;
                    }
                    if ((component.SizeAlignment & Alignment.Down) == Alignment.Down)
                    {
                        targetPosition.z -= aabb.extents.z;
                    }
                    if ((component.SizeAlignment & Alignment.Up) == Alignment.Up)
                    {
                        targetPosition.z += aabb.extents.z;
                    }
                }
                int3 location = grid.PositionToLocation(targetPosition);

                for (int y = 0; y < component.FixedSize.y; y++)
                {
                    for (int x = 0; x < component.FixedSize.x; x++)
                    {
                        for (int z = 0; z < component.FixedSize.z; z++)
                        {
                            ulong index = grid.LocationToIndex(location + new int3(x, y, z));

                            indices.Add(index, entity);
                            entities.Add(entity, index);
                        }
                    }
                }
            }
        }
        private struct PostChangedEventJob : IJob
        {
            private NativeMultiHashMap<InstanceID, ulong> entities;
            private PresentationSystemID<EventSystem> m_EventSystemID;

            public PostChangedEventJob(
                ref NativeMultiHashMap<InstanceID, ulong> entities, EventSystem eventSystem)
            {
                this.entities = entities;
                m_EventSystemID = eventSystem.SystemID;
            }

            public void Execute()
            {
                EventSystem eventSystem = m_EventSystemID.System;
                var array = entities.GetKeyArray(AllocatorManager.Temp);

                for (int i = 0; i < array.Length; i++)
                {
                    var component = array[i].GetComponentReadOnly<GridComponent>();
                    eventSystem.PostEvent(
                        OnGridLocationChangedEvent.GetEvent(
                            array[i], new FixedList4096Bytes<GridIndex>(), component.Indices, true));
                }
            }
        }
        private struct FullObserverUpdateJob : IJobParallelForEntities<GridDetectorComponent>
        {
            [ReadOnly]
            public WorldGrid grid;
            [ReadOnly]
            public NativeMultiHashMap<InstanceID, ulong> entities;
            [WriteOnly]
            public NativeMultiHashMap<GridIndex, InstanceID>.ParallelWriter observers;

            public void Execute(in InstanceID entity, ref GridDetectorComponent detector)
            {
                // TODO : temp
                entities.TryGetFirstValue(entity, out ulong index, out var iter);

                detector.m_ObserveIndices = new FixedList4096Bytes<GridIndex>();

                foreach (var item in grid.GetRange(new GridIndex(grid, index), new int3(detector.DetectedRange, 0, detector.DetectedRange)))
                {
                    observers.Add(item, entity);
                    detector.m_ObserveIndices.Add(item);
                }
            }
        }

        #endregion

        public void InitializeGrid(in AABB aabb, in float cellSize)
        {
            m_Grid.aabb = aabb;
            m_Grid.cellSize = cellSize;

            FullIndexingUpdate();
        }

        #region Grid Entity

        public void UpdateEntity(in InstanceID entity)
        {
            m_GridUpdateJob.Complete();

            Remove(entity);
            ref GridComponent component = ref entity.GetComponent<GridComponent>();
            FixedList4096Bytes<GridIndex> prev = component.Indices;
            Add(entity, ref component, out bool changed);

            if (changed)
            {
                m_EventSystem.PostEvent(
                    OnGridLocationChangedEvent.GetEvent(entity, prev, component.Indices, false));
            }
        }
        public bool HasEntityAt(in GridIndex index)
        {
            return m_Indices.ContainsKey(index.Index);
        }
        public bool TryGetEntityAt(in GridIndex index, out InstanceID entity)
        {
            if (!m_Indices.TryGetFirstValue(index.Index, out entity, out _))
            {
                entity = InstanceID.Empty;
                return false;
            }

            return true;
        }
        public bool TryGetEntitiesAt(in GridIndex index, out EntitiesAtIndexEnumerator iter)
        {
            if (!HasEntityAt(index))
            {
                iter = default(EntitiesAtIndexEnumerator);
                return false;
            }

            iter = new EntitiesAtIndexEnumerator(m_Indices, index.Index);
            return true;
        }
        public IndicesOfEntityEnumerator GetIndiceOfEntity(in InstanceID entity)
        {
            return new IndicesOfEntityEnumerator(
                m_Grid,
                m_Entities,
                entity
                );
        }
        public bool TryGetIndiceOfEntity(in InstanceID entity, ref NativeList<int3> output)
        {
            output.Clear();
            if (!m_Entities.TryGetFirstValue(entity, out ulong index, out var iter))
            {
                return false;
            }

            do
            {
                output.Add(m_Grid.IndexToLocation(index));
            } while (m_Entities.TryGetNextValue(out index, ref iter));

            return true;
        }

        /// <summary>
        /// 해당 엔티티가 그리드에서 점유중인 인덱스들의 총 AABB 를 반환합니다.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="aabb"></param>
        /// <returns></returns>
        public bool TryGetIndicesAABBOfEntity(in InstanceID entity, out AABB aabb)
        {
            if (!m_Entities.TryGetFirstValue(entity, out ulong index, out var iter))
            {
                aabb = AABB.Zero;
                return false;
            }

            float3 tempPos = m_Grid.IndexToPosition(in index);
            aabb = new AABB(tempPos, 0);

            while (m_Entities.TryGetNextValue(out index, ref iter))
            {
                tempPos = m_Grid.IndexToPosition(in index);
                aabb.Encapsulate(tempPos);
            }

            return true;
        }

        #endregion

        #region Enumerator

        [BurstCompatible]
        public struct IndexEnumerator<TPredicate> : IEnumerator<GridIndex>, IEnumerable<GridIndex>
            where TPredicate : struct, IExecutable<GridIndex>
        {
            private NativeMultiHashMap<GridIndex, InstanceID>.KeyValueEnumerator m_Iterator;
            private TPredicate m_Predicate;

            public IndexEnumerator(
                NativeMultiHashMap<GridIndex, InstanceID>.KeyValueEnumerator iter,
                TPredicate predicate)
            {
                m_Iterator = iter;
                m_Predicate = predicate;
            }

            public GridIndex Current => m_Iterator.Current.Key;
            object IEnumerator.Current => m_Iterator.Current.Key;

            public IEnumerator<GridIndex> GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;

            public void Dispose()
            {
                m_Iterator.Dispose();
            }
            public bool MoveNext()
            {
                while (m_Iterator.MoveNext())
                {
                    if (m_Predicate.Predicate(Current)) return true;
                }
                return false;
            }
            public void Reset()
            {
                m_Iterator.Reset();
            }
        }
        [BurstCompatible]
        public struct IndicesOfEntityEnumerator : IEnumerable<GridIndex>
        {
            private WorldGrid m_Grid;
            private NativeMultiHashMap<InstanceID, ulong> m_HashMap;
            private InstanceID m_Target;

            internal IndicesOfEntityEnumerator(
                WorldGrid grid,
                NativeMultiHashMap<InstanceID, ulong> hashMap,
                InstanceID target)
            {
                m_Grid = grid;
                m_HashMap = hashMap;
                m_Target = target;
            }

            public IEnumerator<GridIndex> GetEnumerator()
            {
                if (!m_HashMap.TryGetFirstValue(m_Target, out ulong index, out var iter))
                {
                    yield break;
                }

                do
                {
                    yield return new GridIndex(m_Grid, index);
                } while (m_HashMap.TryGetNextValue(out index, ref iter));
            }
            [NotBurstCompatible]
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        [BurstCompatible]
        public struct EntitiesAtIndexEnumerator : IEnumerable<InstanceID>
        {
            private NativeMultiHashMap<ulong, InstanceID> m_Iterator;
            private ulong m_Index;

            public EntitiesAtIndexEnumerator(NativeMultiHashMap<ulong, InstanceID> hashMap, ulong index)
            {
                m_Iterator = hashMap;
                m_Index = index;
            }

            [NotBurstCompatible]
            public IEnumerator<InstanceID> GetEnumerator()
            {
                if (!m_Iterator.TryGetFirstValue(m_Index, out InstanceID entity, out var iter))
                {
                    yield break;
                }

                do
                {
                    yield return entity;
                } while (m_Iterator.TryGetNextValue(out entity, ref iter));
            }
            [NotBurstCompatible]
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>
            /// <typeparamref name="TComponent"/> 를 가진 엔티티만 반환하는 iterator 입니다.
            /// </summary>
            /// <typeparam name="TComponent"></typeparam>
            /// <returns></returns>
            [NotBurstCompatible]
            public IEnumerable<InstanceID> HasComponent<TComponent>()
                where TComponent : unmanaged, IEntityComponent
            {
                if (!m_Iterator.TryGetFirstValue(m_Index, out InstanceID entity, out var iter))
                {
                    yield break;
                }

                do
                {
                    if (!entity.HasComponent<TComponent>()) continue;
                    
                    yield return entity;
                } while (m_Iterator.TryGetNextValue(out entity, ref iter));
            }
        }

        #endregion

        #region Utils

        public bool ValidateIndex(in GridIndex index)
        {
            if (!m_Grid.m_CheckSum.Equals(index.m_CheckSum) || !m_Grid.Contains(index.Index))
            {
                return false;
            }
            return true;
        }
        /// <inheritdoc cref="BurstGridMathematics.getOutcoastLocations"/>
        public void GetOutcoastLocations(in NativeArray<GridIndex> locations, ref NativeList<GridIndex> result)
        {
            UnsafeFixedListWrapper<GridIndex> temp2 = result.ConvertToFixedWrapper();
            BurstGridMathematics.getOutcoastLocations(m_Grid.aabb, CellSize, locations, ref temp2);

            temp2.CopyToNativeList(ref result);
        }
        /// <inheritdoc cref="BurstGridMathematics.getOutcoastLocationVertices"/>
        public void GetOutcoastVertices(
            in NativeArray<GridIndex> locations,
            ref NativeList<float3> result,
            NativeArray<GridIndex> indicesMap = default)
        {
            if (result.Capacity < locations.Length * 4)
            {
                result.Resize(locations.Length * 4, NativeArrayOptions.UninitializedMemory);
            }

            UnsafeFixedListWrapper<float3> temp = result.ConvertToFixedWrapper();
            BurstGridMathematics.getOutcoastLocationVertices(
                m_Grid.aabb,
                CellSize,
                locations,
                ref temp,
                indicesMap
                );

            temp.CopyToNativeList(ref result);
        }
        public float3x2 GetLineVerticesOf(in GridIndex index, Direction direction)
        {
            AABB aabb;
            unsafe
            {
                BurstGridMathematics.indexToAABB(m_Grid.aabb, CellSize, index.Index, &aabb);
            }
            AABB.Vertices vertices = aabb.vertices;

            float
                cellHalf = CellSize * .5f;

            float3
                pos = IndexToPosition(in index);

            if ((direction & Direction.Up) == Direction.Up)
            {
                pos.y += CellSize;
            }

            if ((direction & Direction.Left) == Direction.Left)
            {
                pos.x -= cellHalf;

                return new float3x2(
                    new float3(pos.x, pos.y, pos.z + cellHalf),
                    new float3(pos.x, pos.y, pos.z - cellHalf)
                    );
            }
            else if ((direction & Direction.Right) == Direction.Right)
            {
                pos.x += cellHalf;

                return new float3x2(
                    new float3(pos.x, pos.y, pos.z - cellHalf),
                    new float3(pos.x, pos.y, pos.z + cellHalf)
                    );
            }

            if ((direction & Direction.Forward) == Direction.Forward)
            {
                pos.z -= cellHalf;

                return new float3x2(
                    new float3(pos.x + cellHalf, pos.y, pos.z),
                    new float3(pos.x - cellHalf, pos.y, pos.z)
                    );
            }
            else if ((direction & Direction.Backward) == Direction.Backward)
            {
                pos.z += cellHalf;

                return new float3x2(
                    new float3(pos.x - cellHalf, pos.y, pos.z),
                    new float3(pos.x + cellHalf, pos.y, pos.z)
                    );
            }

            return 0;
        }

        #endregion

        #region Get Range

        public enum SortOption
        {
            None,

            CloseDistance
        }

        public struct RangeEnumerator : IEnumerable<GridIndex>
        {
            private WorldGrid m_Grid;
            private GridIndex m_From;
            private int3 m_Range;

            internal RangeEnumerator(
                in WorldGrid grid, 
                in GridIndex from, 
                in int3 range)
            {
                m_Grid = grid;
                m_From = from;
                m_Range = range;
            }

            public IEnumerator<GridIndex> GetEnumerator()
            {
                int3
                    location = m_From.Location,
                    minRange, maxRange;
                m_Grid.GetMinMaxLocation(out minRange, out maxRange);

                int
                    minX = location.x - m_Range.x < 0 ? 0 : math.min(location.x - m_Range.x, maxRange.x),
                    maxX = location.x + m_Range.x < 0 ? 0 : math.min(location.x + m_Range.x, maxRange.x),

                    minY = location.y - m_Range.y < 0 ?
                        math.max(location.y - m_Range.y, minRange.y) : math.min(location.y - m_Range.y, maxRange.y),
                    maxY = location.y + m_Range.y < 0 ?
                        math.max(location.y + m_Range.y, minRange.y) : math.min(location.y + m_Range.y, maxRange.y),

                    minZ = location.z - m_Range.z < 0 ? 0 : math.min(location.z - m_Range.z, maxRange.z),
                    maxZ = location.z + m_Range.z < 0 ? 0 : math.min(location.z + m_Range.z, maxRange.z);

                int3
                    start = new int3(minX, minY, minZ),
                    end = new int3(maxX, maxY, maxZ);
                
                int count = 0;
                for (int y = start.y; y < end.y + 1; y++)
                {
                    for (int x = start.x; x < end.x + 1; x++)
                    {
                        for (int z = start.z;
                            z < end.z;
                            z++, count++)
                        {
                            int3 target = new int3(x, y, z);
                            if (!m_Grid.Contains(in target)) continue;

                            yield return new GridIndex(m_Grid.m_CheckSum, target);
                        }
                    }
                }
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public RangeEnumerator GetRange(in InstanceID from,
            in int3 range)
        {
            CompleteGridJob();

            if (!m_Entities.TryGetFirstValue(from, out ulong index, out var iter))
            {
                "err".ToLogError();
                return default(RangeEnumerator);
            }

            // TODO : Temp code
            return GetRange(new GridIndex(m_Grid.m_CheckSum, index), in range);
        }
        public RangeEnumerator GetRange(in GridIndex from,
            in int3 range)
        {
            return new RangeEnumerator(in m_Grid, in from, in range);
        }

        [Obsolete]
        [SkipLocalsInit]
        public void GetRange(in GridIndex from,
            in int3 range,
            ref NativeList<GridIndex> output,
            SortOption sortOption = SortOption.None)
        {
            if (!ValidateIndex(in from))
            {
                "err".ToLogError();
                return;
            }

            int maxCount = ((range.x * 2) + 1) * ((range.z * 2) + 1) * ((range.y * 2) + 1);
            if (maxCount > 255)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"You\'re trying to get range of grid that exceeding length 255. " +
                    $"Buffer is fixed to 255 length, overloading indices({maxCount - 255}) will be dropped.");
            }

            output.Clear();

            int3
                location = from.Location,
                minRange, maxRange;
            m_Grid.GetMinMaxLocation(out minRange, out maxRange);

            int
                minX = location.x - range.x < 0 ? 0 : math.min(location.x - range.x, maxRange.x),
                maxX = location.x + range.x < 0 ? 0 : math.min(location.x + range.x, maxRange.x),

                minY = location.y - range.y < 0 ?
                    math.max(location.y - range.y, minRange.y) : math.min(location.y - range.y, maxRange.y),
                maxY = location.y + range.y < 0 ?
                    math.max(location.y + range.y, minRange.y) : math.min(location.y + range.y, maxRange.y),

                minZ = location.z - range.z < 0 ? 0 : math.min(location.z - range.z, maxRange.z),
                maxZ = location.z + range.z < 0 ? 0 : math.min(location.z + range.z, maxRange.z);

            int3
                start = new int3(minX, minY, minZ),
                end = new int3(maxX, maxY, maxZ),

                dir = end - start;

            unsafe
            {
                int3* buffer = stackalloc int3[maxCount];
                int count = 0;
                for (int y = start.y; y < end.y + 1 && count < maxCount; y++)
                {
                    for (int x = start.x; x < end.x + 1 && count < maxCount; x++)
                    {
                        for (int z = start.z;
                            z < end.z + 1 && count < maxCount;
                            z++, count++)
                        {
                            buffer[count] = new int3(x, y, z);
                        }
                    }
                }

                if (sortOption == SortOption.CloseDistance)
                {
                    UnsafeBufferUtility.Sort(buffer, count, new CloseDistanceComparer(location));
                }

                for (int i = 0; i < count && i < 255; i++)
                {
                    output.Add(new GridIndex(m_Grid.m_CheckSum, buffer[i]));
                }
            }
        }
        [Obsolete]
        public void GetRange(in InstanceID from,
            in int3 range,
            ref FixedList4096Bytes<GridIndex> output,
            SortOption sortOption = SortOption.None)
        {
            CompleteGridJob();

            if (!m_Entities.TryGetFirstValue(from, out ulong index, out var iter))
            {
                "err".ToLogError();
                return;
            }

            // TODO : Temp code
            GetRange(new GridIndex(m_Grid.m_CheckSum, index), range, ref output, sortOption);
        }
        [Obsolete]
        [SkipLocalsInit]
        public void GetRange(in GridIndex from,
            in int3 range,
            ref FixedList4096Bytes<GridIndex> output,
            SortOption sortOption = SortOption.None)
        {
            if (!ValidateIndex(in from))
            {
                "err".ToLogError();
                return;
            }

            int maxCount = ((range.x * 2) + 1) * ((range.z * 2) + 1) * ((range.y * 2) + 1);
            if (maxCount > 255)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"You\'re trying to get range of grid that exceeding length 255. " +
                    $"Buffer is fixed to 255 length, overloading indices({maxCount - 255}) will be dropped.");
            }

            output.Clear();

            int3
                location = from.Location,
                minRange, maxRange;
            m_Grid.GetMinMaxLocation(out minRange, out maxRange);

            int
                minX = location.x - range.x < 0 ? 0 : math.min(location.x - range.x, maxRange.x),
                maxX = location.x + range.x < 0 ? 0 : math.min(location.x + range.x, maxRange.x),

                minY = location.y - range.y < 0 ?
                    math.max(location.y - range.y, minRange.y) : math.min(location.y - range.y, maxRange.y),
                maxY = location.y + range.y < 0 ?
                    math.max(location.y + range.y, minRange.y) : math.min(location.y + range.y, maxRange.y),

                minZ = location.z - range.z < 0 ? 0 : math.min(location.z - range.z, maxRange.z),
                maxZ = location.z + range.z < 0 ? 0 : math.min(location.z + range.z, maxRange.z);
            //if (log) $"minmax: {minRange}, {maxRange}".ToLog();
            int3
                start = new int3(minX, minY, minZ),
                end = new int3(maxX, maxY, maxZ),
                
                dir = end - start;

            
            unsafe
            {
                int3* buffer = stackalloc int3[maxCount];
                int count = 0;
                for (int y = start.y; y < end.y + 1 && count < maxCount; y++)
                {
                    for (int x = start.x; x < end.x + 1 && count < maxCount; x++)
                    {
                        for (int z = start.z;
                            z < end.z + 1 && count < maxCount;
                            z++, count++)
                        {
                            buffer[count] = new int3(x, y, z);
                        }
                    }
                }
                //if (log) $"{count} < {maxCount}: {start}, {end}".ToLog();
                if (sortOption == SortOption.CloseDistance)
                {
                    UnsafeBufferUtility.Sort(buffer, count, new CloseDistanceComparer(location));
                }

                for (int i = 0; i < count && i < 255; i++)
                {
                    output.Add(new GridIndex(m_Grid.m_CheckSum, buffer[i]));
                }
            }
        }

        #endregion

        #region Index

        public bool HasDirection(in GridIndex index, in Direction direction)
        {
            int3 location = CalculateDirection(in index, in direction);

            return m_Grid.Contains(location);
        }
        public GridIndex GetDirection(in GridIndex index, in Direction direction)
        {
            int3 location = CalculateDirection(in index, in direction);
            return new GridIndex(m_Grid.m_CheckSum, location);
        }
        public GridIndex GetDirection(in int3 location, in Direction direction)
        {
            int3 output;
            unsafe
            {
                BurstGridMathematics.getDirection(location, in direction, &output);
            }
            return new GridIndex(m_Grid.m_CheckSum, output);
        }
        public bool TryGetDirection(in GridIndex index, in Direction direction, out GridIndex result)
        {
            // TODO : 임시로 막음. 위아래
            if ((direction & Direction.Up) == Direction.Up || 
                (direction & Direction.Down) == Direction.Down)
            {
                result = default(GridIndex);
                return false;
            }

            int3 location = CalculateDirection(in index, in direction);

            if (!m_Grid.Contains(location))
            {
                result = default(GridIndex);
                return false;
            }

            result = new GridIndex(m_Grid.m_CheckSum, location);
            return true;
        }
        private static unsafe int3 CalculateDirection(in GridIndex index, in Direction direction)
        {
            int3 location;
            BurstGridMathematics.getDirection(index.Location, in direction, &location);
            return location;
        }

        // TODO : point 가 그리드 좌표에서 변환한 좌표값이 아니면 예상한 값이 아닐 확률이 높음.
        // 나중에 radian 으로 연산을 수정할 것
        private Direction GetNormalizedDirection(in float3 point, in float3 center, in quaternion rot)
        {
            float4x4 mat = math.fastinverse(float4x4.TRS(center, rot, 1));
            float3
                xzProj = math.mul(mat, new float4(point, 1)).xyz;

            Direction result = Direction.NONE;
            if (xzProj.x > 0.01f)
            {
                result = Direction.Right;
            }
            else if (xzProj.x < -0.01f)
            {
                result = Direction.Left;
            }

            if (xzProj.z > 0.01f)
            {
                result |= Direction.Backward;
            }
            else if (xzProj.z < -0.01f)
            {
                result |= Direction.Forward;
            }

            if (xzProj.y > 0.01f)
            {
                result |= Direction.Up;
            }
            else if (xzProj.y < -0.01f)
            {
                result |= Direction.Down;
            }
            //$"123,, {xzProj} :: {center}, {point}".ToLog();

            return result;
        }
        public Direction GetReletiveDirectionFrom(in GridIndex index, in GridIndex from, in quaternion rot)
        {
            if (index.Equals(from))
            {
                "?? same".ToLog();
                return Direction.NONE;
            }

            float3 gridPos = IndexToPosition(in index);

            AABB fromAAbb;
            unsafe
            {
                BurstGridMathematics.indexToAABB(m_Grid.aabb, CellSize, from.Index, &fromAAbb);
            }
            var planes = fromAAbb.planes;

            Direction direction;
            for (int i = 0; i < 6; i++)
            {
                if (!planes[i].GetSide(gridPos)) continue;

                direction = (Direction)(1 << i);

                int3 oppoLoc;
                float3 pos;
                unsafe
                {
                    BurstGridMathematics.getDirection(index.Location, direction.GetOpposite(), &oppoLoc);
                    BurstGridMathematics.locationToPosition(m_Grid.aabb, CellSize, oppoLoc, &pos);
                }

                var temp = GetNormalizedDirection(
                    pos,
                    gridPos,
                    rot
                    );
                return temp;
            }

            return Direction.NONE;
        }
        public float3 IndexToPosition(in GridIndex index)
        {
            return m_Grid.IndexToPosition(index.Index);
        }
        public float3 LocationToPosition(in int3 location)
        {
            return m_Grid.LocationToPosition(in location);
        }
        public GridIndex LocationToIndex(in int3 location)
        {
            return new GridIndex(m_Grid, m_Grid.LocationToIndex(location));
        }
        public GridIndex PositionToIndex(in float3 position)
        {
            ulong index = m_Grid.PositionToIndex(in position);
            return new GridIndex(m_Grid, index);
        }

        #endregion

        #region Path

        public bool HasPath(
            in GridIndex from,
            in GridIndex to,
            out int pathFound,
            in int maxIteration = 32
            )
            => GetModule<WorldGridPathModule>().HasPath(in from, in to, out pathFound, in maxIteration);
        public bool GetPath(
            in GridIndex from,
            in GridIndex to,
            ref FixedList4096Bytes<GridIndex> foundPath,
            out int tileCount,
            in int maxIteration = 32
            )
            => GetModule<WorldGridPathModule>().GetPath(in from, in to, ref foundPath, out tileCount, in maxIteration);
        // TODO : Temp code
        public bool GetPath(
            in InstanceID from,
            in GridIndex to,
            ref FixedList4096Bytes<GridIndex> foundPath,
            out int tileCount,
            in int maxIteration = 32
            )
        {
            tileCount = 0;
            if (!m_Entities.TryGetFirstValue(from, out ulong index, out var iter)) return false;

            return GetModule<WorldGridPathModule>().GetPath(new GridIndex(m_Grid.m_CheckSum, index), in to, ref foundPath, out tileCount, in maxIteration);
        }

        #endregion

        #region Cursor

        private GridIndex m_CurrentOverlayIndex;
//        public GridIndex CurrentOverlayIndex
//        {
//            get
//            {
//#if DEBUG_MODE
//                if (!m_EnabledCursorObserve)
//                {
//                    CoreSystem.Logger.LogError(Channel.Presentation,
//                        $"You\'re trying to get grid index at cursor but currently not observing cursor. You should enable observation with {nameof(EnableCursorObserve)} method.");
//                }
//#endif
//                return m_CurrentOverlayIndex;
//            }
//        }

        private void UpdateCursorObservation()
        {
            if (!m_EnabledCursorObserve) return;

            if (!TryGetGridIndexAtCursor(out GridIndex index))
            {
                m_CurrentOverlayIndex = default(GridIndex);
                return;
            }

            if (!m_CurrentOverlayIndex.Equals(index))
            {
                m_CurrentOverlayIndex = index;
                //$"pointing {index}, {info.point}".ToLog();

                m_EventSystem.PostEvent(OnGridCellCursorOverrapEvent.GetEvent(m_CurrentOverlayIndex));
            }

            if (m_InputSystem.IsCursorPressedInThisFrame)
            {
                //$"press {index}, {info.point}".ToLog();
                m_EventSystem.PostEvent(OnGridCellPreseedEvent.GetEvent(m_CurrentOverlayIndex));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// <seealso cref="OnGridCellCursorOverrapEvent"/>, <seealso cref="OnGridCellPreseedEvent"/>
        /// </remarks>
        /// <param name="enable"></param>
        public void EnableCursorObserve(bool enable)
        {
            m_EnabledCursorObserve = enable;

            m_CurrentOverlayIndex = default(GridIndex);
        }
        public bool TryGetGridIndexAtCursor(out GridIndex index)
        {
            Ray ray = m_InputSystem.CursorRay;

            if (!m_LevelDesignSystem.Raycast(ray, out var info) || 
                !m_Grid.Contains(info.point))
            {
                //$"retrn {info.point}".ToLog();
                index = default(GridIndex);
                return false;
            }

            ulong temp = m_Grid.PositionToIndex(info.point);
            index = new GridIndex(m_Grid.m_CheckSum, temp);

            return true;
        }

        #endregion

        #region Detector

        public bool IsObserveIndexOf(in GridIndex index, in InstanceID entity)
        {
            return GetModule<GridDetectorModule>().IsObserveIndexOf(in index, in entity);
        }
        public bool IsObserveIndexOfOnly(in GridIndex index, in InstanceID entity)
        {
            return GetModule<GridDetectorModule>().IsObserveIndexOfOnly(in index, in entity);
        }

        public int GetObserverIndicesCount()
        {
            CompleteGridJob();

            return GetModule<GridDetectorModule>().GridObservers.Count();
        }
        public NativeArray<GridIndex> GetObserverIndices(AllocatorManager.AllocatorHandle allocator)
        {
            CompleteGridJob();

            return GetModule<GridDetectorModule>().GridObservers.GetKeyArray(allocator);
        }
        public IndexEnumerator<AlwaysTrue<GridIndex>> GetObserverIndices()
        {
            CompleteGridJob();

            return new IndexEnumerator<AlwaysTrue<GridIndex>>
                (GetModule<GridDetectorModule>().GridObservers.GetEnumerator(),
                new AlwaysTrue<GridIndex>()
                );
        }
        public IndexEnumerator<TPredicate> GetObserverIndices<TPredicate>(TPredicate predicate)
            where TPredicate : struct, IExecutable<GridIndex>
        {
            CompleteGridJob();

            return new IndexEnumerator<TPredicate>
                (GetModule<GridDetectorModule>().GridObservers.GetEnumerator(),
                predicate
                );
        }


        #endregion
    }
}
