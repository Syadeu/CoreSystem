using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;

namespace Syadeu.Presentation.Map
{
    public sealed class NavMeshSystem : PresentationSystemEntity<NavMeshSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly List<NavMeshComponent> m_Agents = new List<NavMeshComponent>();
        private readonly List<NavObstacleAttribute> m_Obstacles = new List<NavObstacleAttribute>();
        private readonly List<NavMeshBuildSource> m_Sources = new List<NavMeshBuildSource>();
        private bool m_RequireReload = false;

        private EventSystem m_EventSystem;

        protected override PresentationResult OnInitialize()
        {
            RequestSystem<EventSystem>(Bind);

            return base.OnInitialize();
        }
        private void Bind(EventSystem other)
        {
            m_EventSystem = other;

            m_EventSystem.AddEvent<OnTransformChangedEvent>(OnTransformChangedEventHandler);
        }
        private void OnTransformChangedEventHandler(OnTransformChangedEvent ev)
        {
            NavObstacleAttribute obstacleAtt = ev.entity.GetAttribute<NavObstacleAttribute>();
            if (obstacleAtt == null) return;

            m_RequireReload = true;
        }
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
                NavMeshBuilder.UpdateNavMeshDataAsync(agent.m_NavMeshData, NavMesh.GetSettingsByID(agent.m_AgentType), m_Sources, agent.m_Bounds);
            }

            m_RequireReload = false;
            return base.BeforePresentation();
        }

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

            component.m_Registered = false;
        }

        public void AddObstacle(NavObstacleAttribute obstacle, ProxyTransform tr, int areaMask = 0)
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddObstacle), ThreadInfo.Unity);

            var setting = tr.prefab.GetObjectSetting();
            if (string.IsNullOrEmpty(setting.m_RefPrefab.AssetGUID))
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"This entity({obstacle.Parent.Name}) is not valid. Cannot be a obstacle.");
                return;
            }
            if (setting.m_RefPrefab.Asset == null)
            {
                //Addressables.LoadAssetAsync<UnityEngine.Object>(setting.m_RefPrefab);
                CoreSystem.Logger.LogError(Channel.Presentation,
                    $"This entity({obstacle.Parent.Name}) has null prefab. Cannot be a obstacle.");
                return;
            }

            if (setting.m_RefPrefab.Asset is GameObject gameObject)
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
            }
            else
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This prefab is not a GameObject. Cannot be a obstacle.");
                return;
            }

            m_Sources.AddRange(obstacle.m_Sources);
            m_Obstacles.Add(obstacle);
            m_RequireReload = true;
        }
        public void RemoveObstacle(NavObstacleAttribute obstacle)
        {
            m_Sources.Clear();
            m_Obstacles.Remove(obstacle);
            m_RequireReload = true;
        }
    }

    public sealed class NavMeshComponent : MonoBehaviour
    {
        internal bool m_Registered = false;
        internal NavMeshData m_NavMeshData;
        internal NavMeshDataInstance m_Handle;

        [SerializeField] internal int m_AgentType = 0;
        [SerializeField] private Vector3 m_Center = Vector3.zero;
        [SerializeField] private Vector3 m_Size = Vector3.one;
        internal Bounds m_Bounds;

        private void Awake()
        {
            m_NavMeshData = new NavMeshData();
            m_Bounds = new Bounds(m_Center, m_Size);
        }
        private void OnEnable()
        {
            CoreSystem.StartUnityUpdate(this, Authoring(true));
        }
        private void OnDisable()
        {
            CoreSystem.StartUnityUpdate(this, Authoring(false));
        }

        private IEnumerator Authoring(bool enable)
        {
            while (!PresentationSystem<NavMeshSystem>.IsValid())
            {
                yield return null;
            }

            if (enable)
            {
                PresentationSystem<NavMeshSystem>.System.AddBaker(this);
            }
            else PresentationSystem<NavMeshSystem>.System.RemoveBaker(this);
        }
    }
}
