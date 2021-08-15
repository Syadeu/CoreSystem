using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace Syadeu.Presentation
{
    public sealed class NavMeshSystem : PresentationSystemEntity<NavMeshSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private NavMeshData m_NavMeshData;

        public void AddBaker(NavMeshComponent component)
        {
            component.m_NavMeshData = new NavMeshData();
            NavMesh.AddNavMeshData(component.m_NavMeshData);
        }

        public void AddObstacle(NavObstacleAttribute obstacle, ProxyTransform tr, int areaMask = 0)
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddObstacle), ThreadInfo.Unity);

            var setting = tr.prefab.GetObjectSetting();
            if (!setting.m_RefPrefab.IsValid() || setting.m_RefPrefab.Asset == null)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This prefab is null. Cannot be a obstacle.");
                return;
            }

            UnityEngine.Object targetObj;
            if (setting.m_RefPrefab.Asset is GameObject gameObject)
            {
                if (obstacle.m_ObstacleType == NavObstacleAttribute.ObstacleType.Mesh)
                {
                    MeshFilter[] meshFilter = gameObject.GetComponentsInChildren<MeshFilter>();
                    NavMeshBuildSource[] sources = new NavMeshBuildSource[meshFilter.Length];
                    
                    for (int i = 0; i < meshFilter.Length; i++)
                    {
                        targetObj = meshFilter[i].sharedMesh;
                        NavMeshBuildSource data = new NavMeshBuildSource
                        {
                            shape = NavMeshBuildSourceShape.Mesh,
                            sourceObject = targetObj,
                            transform = tr.localToWorldMatrix,
                            area = areaMask
                        };

                        sources[i] = data;
                    }
                    obstacle.m_Sources = sources;
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
                    obstacle.m_Sources = new NavMeshBuildSource[] { source };
                }
            }
            else
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "This prefab is not a GameObject. Cannot be a obstacle.");
                return;
            }

            

            CoreSystem.Logger.Unmanaged<NavMeshBuildSource>();
        }
    }

    public sealed class NavMeshComponent : MonoBehaviour
    {
        internal NavMeshData m_NavMeshData;

        private void Awake()
        {
            m_NavMeshData = new NavMeshData();
        }
        private void OnEnable()
        {
            
        }
        private void OnDisable()
        {
            
        }
    }
}
