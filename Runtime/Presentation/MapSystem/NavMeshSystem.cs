using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using System;
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

        private readonly List<NavMeshComponent> m_Agents = new List<NavMeshComponent>();
        private readonly List<NavObstacleAttribute> m_Obstacles = new List<NavObstacleAttribute>();
        private readonly List<NavMeshBuildSource> m_Sources = new List<NavMeshBuildSource>();
        private bool m_RequireReload = false;

        private EventSystem m_EventSystem;

        #region Presentation Methods

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<EventSystem>(Bind);

            return base.OnInitialize();
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

        #endregion

        public override void OnDispose()
        {
            m_EventSystem.RemoveEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);

            m_EventSystem = null;
        }
        protected override PresentationResult BeforePresentation()
        {
            if (!m_RequireReload) return base.BeforePresentation();

            if (m_Sources.Count == 0)
            {
                foreach (NavMeshBuildSource[] item in m_Obstacles.Select((other) => other.m_Sources))
                {
                    m_Sources.AddRange(item);
                }
            }
            
            foreach (NavMeshComponent agent in m_Agents)
            {
                Bounds bounds = agent.Bounds;

                NavMeshBuilder.UpdateNavMeshDataAsync(agent.m_NavMeshData, NavMesh.GetSettingsByID(agent.m_AgentType), m_Sources, 
                    QuantizedBounds(bounds.center + agent.transform.position, bounds.size));
            }

            m_RequireReload = false;
            return base.BeforePresentation();
        }

        #endregion

        public void AddBaker(NavMeshComponent component)
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
        public void RemoveBaker(NavMeshComponent component)
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
            m_Sources.Clear();
            m_Obstacles.Remove(obstacle);
            m_RequireReload = true;
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
}
