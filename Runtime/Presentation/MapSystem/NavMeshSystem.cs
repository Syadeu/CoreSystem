using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
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
        private GridSystem m_GridSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, EventSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, GridSystem>(Bind);

            //PoolContainer<NavMeshQueryContainer>.Initialize(NavMeshQueryFactory, 16);

            return base.OnInitialize();
        }
        public override void OnDispose()
        {
            //PoolContainer<NavMeshQueryContainer>.Dispose();

            m_EventSystem.RemoveEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);

            m_EventSystem = null;
            m_CoroutineSystem = null;
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
        private void Bind(GridSystem other)
        {
            m_GridSystem = other;
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
            
            foreach (NavMeshBaker agent in m_Agents)
            {
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

        public void MoveTo(Entity<IEntity> entity, float3 point, ActorMoveEvent ev)
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                "no agent".ToLogError();
                return;
            }

            NativeList<float3> position = new NativeList<float3>(1, Allocator.Persistent);
            position.Add(point);
            MoveJob moveJob = new MoveJob()
            {
                m_Entity = entity.As<IEntity,IEntityData>(),
                m_Positions = position
            };
            m_CoroutineSystem.PostCoroutineJob(moveJob);

            entity.GetComponent<ActorControllerComponent>().ScheduleEvent(ev);
        }
        public void MoveTo(Entity<IEntity> entity, GridPath32 path, ActorMoveEvent ev)
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                "no agent".ToLogError();
                return;
            }

            NativeList<float3> position = new NativeList<float3>(path.Length, Allocator.Persistent);
            for (int i = 0; i < path.Length; i++)
            {
                position.Add(m_GridSystem.IndexToPosition(path[i].index));
            }

            MoveJob moveJob = new MoveJob()
            {
                m_Entity = entity.As<IEntity,IEntityData>(),
                m_Positions = position
            };
            m_CoroutineSystem.PostCoroutineJob(moveJob);

            entity.GetComponent<ActorControllerComponent>().ScheduleEvent(ev);
        }
        public void MoveTo(Entity<IEntity> entity, IList<float3> points, ActorMoveEvent ev)
        {
            NavAgentAttribute navAgent = entity.GetAttribute<NavAgentAttribute>();
            if (navAgent == null)
            {
                "no agent".ToLogError();
                return;
            }

            NativeList<float3> position = new NativeList<float3>(points.Count, Allocator.Persistent);
            for (int i = 0; i < points.Count; i++)
            {
                position.Add(points[i]);
            }

            MoveJob moveJob = new MoveJob()
            {
                m_Entity = entity.As<IEntity,IEntityData>(),
                m_Positions = position
            };
            m_CoroutineSystem.PostCoroutineJob(moveJob);

            entity.GetComponent<ActorControllerComponent>().ScheduleEvent(ev);
        }
        private struct MoveJob : ICoroutineJob
        {
            public EntityData<IEntityData> m_Entity;
            public NativeList<float3> m_Positions;

            public UpdateLoop Loop => UpdateLoop.Transform;

            public void Dispose()
            {
                m_Positions.Dispose();
            }
            private void SetPreviousPosition(float3 pos)
            {
                ref NavAgentCompoonent agent = ref m_Entity.GetComponent<NavAgentCompoonent>();
                agent.m_PreviousTarget = pos;
            }
            private void SetIsMoving(bool moving)
            {
                ref NavAgentCompoonent agent = ref m_Entity.GetComponent<NavAgentCompoonent>();
                agent.m_IsMoving = moving;
            }
            public IEnumerator Execute()
            {
                EventSystem eventSystem = PresentationSystem<DefaultPresentationGroup, EventSystem>.System;
                NavAgentAttribute navAgent = m_Entity.GetAttribute<NavAgentAttribute>();
                Entity<IEntity> entity = m_Entity.As<IEntityData, IEntity>();
                ProxyTransform tr = (ProxyTransform)entity.transform;

                if (!tr.hasProxy)
                {
                    SetPreviousPosition(m_Positions[m_Positions.Length - 1]);

                    tr.position = m_Positions[m_Positions.Length - 1];
                    eventSystem.PostEvent(
                        OnMoveStateChangedEvent.GetEvent(entity,
                            OnMoveStateChangedEvent.MoveState.Teleported | OnMoveStateChangedEvent.MoveState.Idle));
                    navAgent.m_OnMoveActions.Execute(m_Entity);

                    m_Positions.Clear();

                    yield break;
                }

                eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(
                        entity, OnMoveStateChangedEvent.MoveState.AboutToMove));

                NavMeshAgent agent = tr.proxy.GetComponent<NavMeshAgent>();
                agent.updatePosition = false;
                //agent.updateRotation = false;

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

                while (tr.hasProxy && m_Positions.Length > 0)
                {
                    if (agent.pathPending)
                    {
                        yield return null;
                        continue;
                    }

                    if (agent.remainingDistance < .25f)
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

                        yield return null;
                        continue;
                    }

                    tr.position = agent.nextPosition;
                    tr.Synchronize(ProxyTransform.SynchronizeOption.Rotation);

                    eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(
                        entity, OnMoveStateChangedEvent.MoveState.OnMoving));
                    navAgent.m_OnMoveActions.Execute(m_Entity);

                    yield return null;
                }

                SetIsMoving(false);
                agent.ResetPath();

                eventSystem.PostEvent(OnMoveStateChangedEvent.GetEvent(entity, 
                    OnMoveStateChangedEvent.MoveState.Stopped | OnMoveStateChangedEvent.MoveState.Idle));
                navAgent.m_OnMoveActions.Execute(m_Entity);
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

    public struct ActorMoveEvent : IActorEvent, IEventSequence
    {
        private Entity<ActorEntity> m_Entity;
        private float m_AfterDelay;

        public bool KeepWait
        {
            get
            {
                NavAgentCompoonent agent = m_Entity.GetComponent<NavAgentCompoonent>();
                return agent.m_IsMoving;
            }
        }
        public float AfterDelay => m_AfterDelay;
        public bool BurstCompile => false;

        public ActorMoveEvent(float afterDelay)
        {
            m_Entity = Entity<ActorEntity>.Empty;
            m_AfterDelay = afterDelay;
        }

        public void OnExecute(Entity<ActorEntity> from)
        {
            m_Entity = from;
        }
    }

    public struct NavAgentCompoonent : IEntityComponent
    {
        internal bool m_IsMoving;
        internal float3 m_Direction;
        internal float m_Speed;
        internal float3 m_PreviousTarget;
    }
}
