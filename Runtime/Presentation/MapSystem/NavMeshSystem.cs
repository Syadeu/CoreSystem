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

using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Grid;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Presentation.Map
{
    public sealed class NavMeshSystem : PresentationSystemEntity<NavMeshSystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly List<NavMeshBaker> m_Agents = new List<NavMeshBaker>();
        private readonly List<NavObstacleAttribute> m_Obstacles = new List<NavObstacleAttribute>();
        private readonly List<TerrainData> m_Terrains = new List<TerrainData>();
        private readonly List<NavMeshBuildSource> m_Sources = new List<NavMeshBuildSource>();
        private bool m_RequireReload = false;

        private EventSystem m_EventSystem;
        private CoroutineSystem m_CoroutineSystem;
        private WorldGridSystem m_GridSystem;
        private ActorSystem m_ActorSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, WorldGridSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, ActorSystem>(Bind);

            //PoolContainer<NavMeshQueryContainer>.Initialize(NavMeshQueryFactory, 16);

            return base.OnInitialize();
        }
        protected override void OnDispose()
        {
            //PoolContainer<NavMeshQueryContainer>.Dispose();

            m_EventSystem.RemoveEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);

            m_EventSystem = null;
            m_CoroutineSystem = null;
            m_GridSystem = null;
            m_ActorSystem = null;
        }

        private sealed class NavMeshQueryContainer : IDisposable
        {
            public NavMeshQuery m_Query;

            public NavMeshQueryContainer()
            {
                m_Query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 256);
            }
            public void Dispose()
            {
                m_Query.Dispose();
            }

            public void asd()
            {
                //m_Query.
            }
        }
        private NavMeshQueryContainer NavMeshQueryFactory()
        {
            return new NavMeshQueryContainer();
        }

        #region Binds

        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }
        private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        {
            if (!ev.entity.IsValid()) return;
            NavObstacleAttribute obstacleAtt = ev.entity.GetAttribute<NavObstacleAttribute>();
            if (obstacleAtt == null) return;

            for (int i = 0; i < obstacleAtt.m_Sources.Length; i++)
            {
                obstacleAtt.m_Sources[i].transform = ev.transform.localToWorldMatrix;
            }

            m_Sources.Clear();
            m_RequireReload = true;
        }

        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }
        private void Bind(WorldGridSystem other)
        {
            m_GridSystem = other;
        }
        private void Bind(ActorSystem other)
        {
            m_ActorSystem = other;
        }

        #endregion

        protected override PresentationResult BeforePresentation()
        {
            if (!m_RequireReload) return base.BeforePresentation();

            if (m_Sources.Count == 0)
            {
                foreach (NavMeshBuildSource[] item in m_Obstacles.Select((other) => other.m_Sources))
                {
                    m_Sources.AddRange(item);
                }
                foreach (NavMeshBuildSource[] item in m_Terrains.Select((other) => other.m_Sources))
                {
                    m_Sources.AddRange(item);
                }
            }

            for (int i = m_Agents.Count - 1; i >= 0; i--)
            {
                if (m_Agents[i] == null)
                {
                    "unhandled destroy navagent".ToLogError();
                    m_Agents.RemoveAt(i);
                    continue;
                }

                NavMeshBaker agent = m_Agents[i];
                Bounds bounds = agent.Bounds;

                NavMeshBuilder.UpdateNavMeshDataAsync(agent.m_NavMeshData, NavMesh.GetSettingsByID(agent.m_AgentType), m_Sources,
                    QuantizedBounds(bounds.center + agent.transform.position, bounds.size));
            }

            m_RequireReload = false;
            return base.BeforePresentation();
        }

        #endregion

        public void AddBaker(NavMeshBaker component)
        {
            if (component.m_Registered)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "error");
                return;
            }

            if (component.m_NavMeshData == null) component.m_NavMeshData = new NavMeshData();
            component.m_Handle = NavMesh.AddNavMeshData(component.m_NavMeshData);

            component.m_Registered = true;
            m_Agents.Add(component);
            NavMeshBuilder.UpdateNavMeshDataAsync(component.m_NavMeshData, NavMesh.GetSettingsByID(component.m_AgentType), m_Sources, component.Bounds);
        }
        public void RemoveBaker(NavMeshBaker component)
        {
            if (!component.m_Registered)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "error");
                return;
            }

            NavMesh.RemoveNavMeshData(component.m_Handle);
            m_Agents.Remove(component);
            component.m_Registered = false;
        }

        public void AddTerrain(TerrainData terrainData, int areaMask)
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddTerrain), ThreadInfo.Unity);

            if (terrainData.m_TerrainInstance == null)
            {
                CoreSystem.Logger.LogError(Channel.Presentation, "Terrain is not an instance");
                return;
            }

            Transform tr = terrainData.m_TerrainInstance.transform;
            NavMeshBuildSource source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Terrain,
                sourceObject = terrainData.m_TerrainInstance.terrainData,
                transform = float4x4.TRS(tr.position, quaternion.identity, 1),
                area = areaMask
            };
            terrainData.m_Sources = new NavMeshBuildSource[] { source };

            m_Sources.AddRange(terrainData.m_Sources);
            m_Terrains.Add(terrainData);
            m_RequireReload = true;
        }
        public void AddObstacle(NavObstacleAttribute obstacle, ITransform transform, int areaMask)
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddObstacle), ThreadInfo.Unity);
            if (!(transform is IProxyTransform tr))
            {
                CoreSystem.Logger.LogError(Channel.Presentation, "unity tr is not support");
                return;
            }

            if (tr.prefab.Asset == null)
            {
                AsyncOperationHandle<GameObject> oper = tr.prefab.LoadAssetAsync<GameObject>();
                oper.Completed += Oper_Completed;
            }
            else Execute(tr.prefab.Asset as GameObject);

            void Oper_Completed(AsyncOperationHandle<GameObject> obj)
            {
                GameObject gameObject = obj.Result;
                Execute(gameObject);
            }
            void Execute(GameObject gameObject)
            {
                if (obstacle.m_Sources == null)
                {
                    NavMeshBuildSource[] sources;
                    if (obstacle.m_ObstacleType == NavObstacleAttribute.ObstacleType.Mesh)
                    {
                        MeshFilter[] meshFilter = gameObject.GetComponentsInChildren<MeshFilter>();
                        sources = new NavMeshBuildSource[meshFilter.Length];

                        for (int i = 0; i < meshFilter.Length; i++)
                        {
                            UnityEngine.Object targetObj = meshFilter[i].sharedMesh;
                            NavMeshBuildSource data = new NavMeshBuildSource
                            {
                                shape = NavMeshBuildSourceShape.Mesh,
                                sourceObject = targetObj,
                                transform = tr.localToWorldMatrix,
                                area = areaMask
                            };

                            sources[i] = data;
                        }
                    }
                    else
                    {
                        Terrain terrain = gameObject.GetComponentInChildren<Terrain>();
                        NavMeshBuildSource source = new NavMeshBuildSource
                        {
                            shape = NavMeshBuildSourceShape.Terrain,
                            sourceObject = terrain.terrainData,
                            transform = float4x4.TRS(tr.position, quaternion.identity, 1),
                            area = areaMask
                        };
                        sources = new NavMeshBuildSource[] { source };
                    }

                    obstacle.m_Sources = sources;
                }

                m_Sources.AddRange(obstacle.m_Sources);
                m_Obstacles.Add(obstacle);
                m_RequireReload = true;
            }
        }
        public void RemoveObstacle(NavObstacleAttribute obstacle)
        {
            CoreSystem.Logger.ThreadBlock(nameof(RemoveObstacle), ThreadInfo.Unity);

            m_Sources.Clear();
            m_Obstacles.Remove(obstacle);
            m_RequireReload = true;
        }
        public void RemoveTerrain(TerrainData terrainData)
        {
            CoreSystem.Logger.ThreadBlock(nameof(RemoveObstacle), ThreadInfo.Unity);

            m_Sources.Clear();
            m_Terrains.Remove(terrainData);
            m_RequireReload = true;
        }

        public ActorEventHandler FixCurrentGridPosition(Entity<IEntity> entity)
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                "no agent".ToLogError();
                return ActorEventHandler.Empty;
            }

            FixedList4096Bytes<float3> position = new FixedList4096Bytes<float3>();
            position.Add(entity.transform.position);
            position.Add(m_GridSystem.IndexToPosition(entity.GetComponent<GridComponent>().Indices[0]));
            ActorMoveEvent ev = new ActorMoveEvent(entity.Idx, 0)
            {
                m_MoveJob = new MoveJob()
                {
                    m_Entity = entity.ToEntity<IEntityData>(),
                    m_Positions = position
                }
            };

            return m_ActorSystem.ScheduleEvent(entity.ToEntity<ActorEntity>(), ev, true);
        }
        public ActorEventHandler MoveTo(Entity<IEntity> entity, float3 point, ActorMoveEvent ev)
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                "no agent".ToLogError();
                return ActorEventHandler.Empty;
            }

            FixedList4096Bytes<float3> position = new FixedList4096Bytes<float3>();
            position.Add(entity.transform.position);
            position.Add(point);
            ev.m_MoveJob = new MoveJob()
            {
                m_Entity = entity.ToEntity<IEntityData>(),
                m_Positions = position
            };

            return m_ActorSystem.ScheduleEvent(entity.ToEntity<ActorEntity>(), ev, true);
        }
        public ActorEventHandler MoveTo<TPredicate>(Entity<IEntity> entity, float3 point, ActorMoveEvent<TPredicate> ev)
            where TPredicate : unmanaged, IExecutable<Entity<ActorEntity>>
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                "no agent".ToLogError();
                return ActorEventHandler.Empty;
            }

            FixedList4096Bytes<float3> position = new FixedList4096Bytes<float3>();
            position.Add(entity.transform.position);
            position.Add(point);
            ev.m_MoveJob = new MoveJob()
            {
                m_Entity = entity.ToEntity<IEntityData>(),
                m_Positions = position
            };

            return m_ActorSystem.ScheduleEvent(entity.ToEntity<ActorEntity>(), ev, true);
        }
        public ActorEventHandler MoveTo(Entity<IEntity> entity, FixedList4096Bytes<GridIndex> path, ActorMoveEvent ev)
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                "no agent".ToLogError();
                return ActorEventHandler.Empty;
            }

            FixedList4096Bytes<float3> position = new FixedList4096Bytes<float3>();
            for (int i = 1; i < path.Length; i++)
            {
                position.Add(m_GridSystem.IndexToPosition(path[i]));
            }

            ev.m_MoveJob = new MoveJob()
            {
                m_Entity = entity.ToEntity<IEntityData>(),
                m_Positions = position
            };

            return m_ActorSystem.ScheduleEvent(entity.ToEntity<ActorEntity>(), ev);
        }
        public ActorEventHandler MoveTo(Entity<IEntity> entity, IList<float3> points, ActorMoveEvent ev)
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                "no agent".ToLogError();
                return ActorEventHandler.Empty;
            }

            FixedList4096Bytes<float3> position = new FixedList4096Bytes<float3>();
            position.Add(entity.transform.position);
            for (int i = 0; i < points.Count; i++)
            {
                position.Add(points[i]);
            }

            ev.m_MoveJob = new MoveJob()
            {
                m_Entity = entity.ToEntity<IEntityData>(),
                m_Positions = position
            };

            return m_ActorSystem.ScheduleEvent(entity.ToEntity<ActorEntity>(), ev);
        }
        internal struct MoveJob : ICoroutineJob
        {
            public Entity<IEntityData> m_Entity;
            public FixedList4096Bytes<float3> m_Positions;

            public UpdateLoop Loop => UpdateLoop.Transform;

            public void Dispose()
            {
                if (!m_Entity.IsValid() || !m_Entity.HasComponent<NavAgentComponent>()) return;

                ref NavAgentComponent agent = ref m_Entity.GetComponent<NavAgentComponent>();

                agent.m_Direction = 0;
                agent.m_IsMoving = false;

                PresentationSystem<DefaultPresentationGroup, EventSystem>.System.PostEvent(OnMoveStateChangedEvent.GetEvent(m_Entity.ToEntity<IEntity>(),
                    OnMoveStateChangedEvent.MoveState.Stopped | OnMoveStateChangedEvent.MoveState.Idle));
                agent.m_OnMoveActions.Execute(m_Entity.ToEntity<IObject>());
            }
            private void SetPreviousPosition(float3 pos)
            {
                ref NavAgentComponent agent = ref m_Entity.GetComponent<NavAgentComponent>();
                agent.m_PreviousTarget = pos;
            }
            private void SetDestination(float3 pos)
            {
                ref NavAgentComponent agent = ref m_Entity.GetComponent<NavAgentComponent>();
                agent.m_Destination = pos;
            }
            private void SetIsMoving(bool moving)
            {
                ref NavAgentComponent agent = ref m_Entity.GetComponent<NavAgentComponent>();
                agent.m_IsMoving = moving;
            }
            private void SetDirection(float3 dir)
            {
                ref NavAgentComponent agent = ref m_Entity.GetComponent<NavAgentComponent>();
                agent.m_Direction = dir;
            }
            private void UpdateNavAgentSpeed(Animator animator, NavMeshAgent agent)
            {
                if (animator == null) return;

                Vector3 v = agent.velocity * Time.deltaTime;
                Vector3 animatorDelta = animator.deltaPosition;

                float rootSpeed = 0f;
                if (Vector3.Angle(animatorDelta, v) < 180f)
                {
                    float vMag = v.magnitude;

                    Vector3 projectedDelta = (Vector3.Dot(animatorDelta, v) / (vMag * vMag)) * v;
                    rootSpeed = projectedDelta.magnitude / Time.deltaTime;
                }

                if (!float.IsNaN(rootSpeed))
                {
                    agent.speed = Mathf.Max(rootSpeed, 0.5f);
                }
            }

            public IEnumerator Execute()
            {
                if (m_Positions.Length == 0)
                {
                    "no path".ToLog();

                    yield break;
                }

                EventSystem eventSystem = PresentationSystem<DefaultPresentationGroup, EventSystem>.System;
                NavAgentComponent navAgent = m_Entity.GetComponentReadOnly<NavAgentComponent>();
                Entity<IEntity> entity = m_Entity.ToEntity<IEntity>();
                ProxyTransform tr = (ProxyTransform)entity.transform;
                NavMeshAgent agent = tr.proxy.GetComponent<NavMeshAgent>();
                if (!agent.isOnNavMesh)
                {
                    agent.enabled = false;
                    agent.enabled = true;

                    if (!agent.isOnNavMesh)
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                        $"This entity({entity.RawName}) is not on NavMesh.");
                        yield break;
                    }
                }

                var animator = m_Entity.GetAttribute<AnimatorAttribute>();
                bool rootMotion = animator != null && animator.AnimatorComponent.RootMotion;

                SetDestination(m_Positions[m_Positions.Length - 1]);

                if (!tr.hasProxy)
                {
                    SetPreviousPosition(m_Positions[m_Positions.Length - 1]);

                    tr.position = m_Positions[m_Positions.Length - 1];
                    eventSystem.PostEvent(
                        OnMoveStateChangedEvent.GetEvent(entity,
                            OnMoveStateChangedEvent.MoveState.Teleported | OnMoveStateChangedEvent.MoveState.Idle));
                    navAgent.m_OnMoveActions.Execute(m_Entity.ToEntity<IObject>());

                    m_Positions.Clear();

                    yield break;
                }

                eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(
                        entity, OnMoveStateChangedEvent.MoveState.AboutToMove));

                
                if (rootMotion)
                {
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                }
                else
                {
                    agent.updatePosition = false;
                    //agent.updateRotation = false;
                }

                if (!agent.isOnNavMesh)
                {
                    agent.enabled = false;
                    agent.enabled = true;
                }

                agent.ResetPath();
                agent.SetDestination(m_Positions[0]);
                SetPreviousPosition(m_Positions[0]);
                SetIsMoving(true);

                float cacheStoppingDis = agent.stoppingDistance;
                if (m_Positions.Length > 1)
                {
                    agent.stoppingDistance = 0;
                    agent.autoBraking = false;
                }

                float pendingStartTime = CoreSystem.time;
                while (agent.pathPending)
                {
                    if (CoreSystem.time - pendingStartTime > 5)
                    {
                        "something is wrong".ToLogError();
                        yield break;
                    }

                    yield return null;
                }

                while (tr.hasProxy && m_Positions.Length > 0 && !agent.isStopped)
                {
                    if (agent.remainingDistance < 1f)
                    {
                        m_Positions.RemoveAt(0);
                        if (m_Positions.Length == 0)
                        {
                            agent.stoppingDistance = cacheStoppingDis;
                            agent.autoBraking = true;
                            break;
                        }

                        agent.SetDestination(m_Positions[0]);
                        SetPreviousPosition(m_Positions[0]);

                        //"1".ToLog();
                        yield return null;
                        continue;
                    }

                    SetDirection(agent.desiredVelocity);
                    //SetDirection(math.normalize((float3)agent.nextPosition - tr.position));

                    if (!rootMotion)
                    {
                        tr.position = agent.nextPosition;
                        tr.Synchronize(IProxyTransform.SynchronizeOption.Rotation);
                    }
                    else
                    {
                        tr.Synchronize(IProxyTransform.SynchronizeOption.TR);
                    }

                    eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(
                        entity, OnMoveStateChangedEvent.MoveState.OnMoving));
                    navAgent.m_OnMoveActions.Execute(m_Entity.ToEntity<IObject>());

                    //"2".ToLog();
                    yield return null;
                }

                //while (tr.hasProxy && agent.remainingDistance > .1f && agent.hasPath)
                //{
                //    SetDirection(agent.desiredVelocity);
                //    //SetDirection(math.normalize((float3)agent.nextPosition - tr.position));

                //    if (!rootMotion)
                //    {
                //        tr.position = agent.nextPosition;
                //        tr.Synchronize(IProxyTransform.SynchronizeOption.Rotation);
                //    }
                //    else
                //    {
                //        tr.Synchronize(IProxyTransform.SynchronizeOption.TR);
                //    }

                //    eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(
                //        entity, OnMoveStateChangedEvent.MoveState.OnMoving));
                //    navAgent.m_OnMoveActions.Execute(m_Entity.ToEntity<IObject>());

                //    "3".ToLog();
                //    yield return null;
                //}

                do
                {
                    SetDirection(agent.desiredVelocity);
                    //SetDirection(math.normalize((float3)agent.nextPosition - tr.position));

                    if (!rootMotion)
                    {
                        tr.position = agent.nextPosition;
                        tr.Synchronize(IProxyTransform.SynchronizeOption.Rotation);
                    }
                    else
                    {
                        tr.Synchronize(IProxyTransform.SynchronizeOption.TR);
                    }

                    eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(
                        entity, OnMoveStateChangedEvent.MoveState.OnMoving));
                    navAgent.m_OnMoveActions.Execute(m_Entity.ToEntity<IObject>());

                    //"4".ToLog();
                    yield return null;
                } while (navAgent.m_UpdateTRSWhile.Length > 0 &&
                        navAgent.m_UpdateTRSWhile.Execute(m_Entity.ToEntity<IObject>(), out bool predicate) && predicate);

                SetDirection(0);
                agent.ResetPath();
            }
        }

        private static float3 Quantize(float3 v, float3 quant)
        {
            float x = quant.x * math.floor(v.x / quant.x);
            float y = quant.y * math.floor(v.y / quant.y);
            float z = quant.z * math.floor(v.z / quant.z);
            return new float3(x, y, z);
        }
        private static Bounds QuantizedBounds(Vector3 center, Vector3 size)
        {
            // Quantize the bounds to update only when theres a 10% change in size
            return new Bounds(Quantize(center, 0.1f * size), size);
        }
    }

    public struct ActorMoveEvent : IActorEvent, IEventSequence, IEquatable<ActorMoveEvent>
    {
        private InstanceID m_Entity;
        private float m_AfterDelay;
        internal NavMeshSystem.MoveJob m_MoveJob;

        public bool KeepWait
        {
            get
            {
                NavAgentComponent agent = m_Entity.GetComponent<NavAgentComponent>();
                return agent.m_MoveJob.Running;
            }
        }
        public float AfterDelay => m_AfterDelay;
        public bool BurstCompile => false;

        public ActorMoveEvent(InstanceID entity, float afterDelay)
        {
            m_Entity = entity;
            m_AfterDelay = afterDelay;

            m_MoveJob = default;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
            ref NavAgentComponent agent = ref m_Entity.GetComponent<NavAgentComponent>();
            agent.m_IsMoving = true;

            if (agent.m_MoveJob.IsValid())
            {
                agent.m_MoveJob.Stop();
            }

            agent.m_MoveJob 
                = PresentationSystem<DefaultPresentationGroup, CoroutineSystem>.System.StartCoroutine(m_MoveJob);
        }
        public bool Equals(ActorMoveEvent other) => m_Entity.Equals(other.m_Entity);
    }
    public struct ActorMoveEvent<TPredicate> : IActorEvent, IEventSequence, IEquatable<ActorMoveEvent<TPredicate>>
        where TPredicate : unmanaged, IExecutable<Entity<ActorEntity>>
    {
        private Entity<IEntityData> m_Entity;
        private float m_AfterDelay;
        internal NavMeshSystem.MoveJob m_MoveJob;
        private TPredicate m_Predicate;

        public bool KeepWait
        {
            get
            {
                NavAgentComponent agent = m_Entity.GetComponent<NavAgentComponent>();
                return agent.m_IsMoving;
            }
        }
        public float AfterDelay => m_AfterDelay;

        public ActorMoveEvent(Entity<IEntityData> entity, float afterDelay, TPredicate predicate)
        {
            m_Entity = entity;
            m_AfterDelay = afterDelay;

            m_MoveJob = default;
            m_Predicate = predicate;
        }
        public ActorMoveEvent(Entity<IEntityData> entity, TPredicate predicate)
        {
            m_Entity = entity;
            m_AfterDelay = 0;

            m_MoveJob = default;
            m_Predicate = predicate;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
            if (!m_Predicate.Predicate(in from)) return;

            ref NavAgentComponent agent = ref m_Entity.GetComponent<NavAgentComponent>();
            agent.m_IsMoving = true;

            if (agent.m_MoveJob.IsValid())
            {
                agent.m_MoveJob.Stop();
            }

            agent.m_MoveJob
                = PresentationSystem<DefaultPresentationGroup, CoroutineSystem>.System.StartCoroutine(m_MoveJob);
        }
        public bool Equals(ActorMoveEvent<TPredicate> other) => m_Entity.Equals(other.m_Entity);
    }

    public struct NavAgentComponent : IEntityComponent
    {
        internal bool m_IsMoving;
        internal float3 m_Direction;
        internal float3 m_PreviousTarget, m_Destination;
        internal CoroutineHandler m_MoveJob;

        internal FixedReferenceList64<TriggerAction> m_OnMoveActions;
        internal FixedReferenceList64<TriggerPredicateAction> m_UpdateTRSWhile;

        public bool IsMoving => m_IsMoving;
        public float Speed => math.sqrt(math.mul(m_Direction, m_Direction));
        public float3 Direction => m_Direction;
    }
}
