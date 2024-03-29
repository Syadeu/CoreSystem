﻿// Copyright 2021 Seung Ha Kim
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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Presentation.Map
{
    [DisplayName("Data: Terrain Data")]
    public sealed class TerrainData : DataObjectBase, INotifyAsset
    {
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 0, PropertyName = "Data")]
        private PrefabReference<UnityEngine.TerrainData> m_Data = PrefabReference<UnityEngine.TerrainData>.None;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 1, PropertyName = "Material")]
        private PrefabReference<UnityEngine.Material> m_Material = PrefabReference<UnityEngine.Material>.None;

        [Space, Header("Transformation")]
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 2, PropertyName = "Position")]
        private float3 m_Position;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 3, PropertyName = "Rotation")]
        private float3 m_Rotation;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 4, PropertyName = "Scale")]
        private float3 m_Scale;

        [Space, Header("Terrain NavObstacle")]
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 5, PropertyName = "EnableObstacle")]
        private bool m_EnableObstacle = false;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 6, PropertyName = "AreaMask")]
        private int m_AreaMask = 0;

        [JsonIgnore] internal Terrain m_TerrainInstance = null;
        [JsonIgnore] private NavMeshSystem m_NavMeshSystem = null; 
        [JsonIgnore] internal NavMeshBuildSource[] m_Sources = null;

        [JsonIgnore] public bool IsCreated => m_TerrainInstance != null;
        [JsonIgnore] public TRS TRS => new TRS(m_Position, m_Rotation, m_Scale);

        IEnumerable<IPrefabReference> INotifyAsset.NotifyAssets
        {
            get => new IPrefabReference[] { m_Data, m_Material };
        }

        protected override void OnCreated()
        {
            if (m_EnableObstacle) m_NavMeshSystem = PresentationSystem<DefaultPresentationGroup, NavMeshSystem>.System;
        }
        protected override void OnDestroy()
        {
            if (m_TerrainInstance != null)
            {
                UnityEngine.Object.Destroy(m_TerrainInstance.gameObject);
            }

            if (m_EnableObstacle)
            {
                m_NavMeshSystem.RemoveTerrain(this);
                m_NavMeshSystem = null;
            }
        }
        public void Create(Action<Terrain> onComplete)
        {
            if (IsCreated)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"{Name} terrain is already created.");
                return;
            }

            if (m_Data.IsNone()) return;
            else if (!m_Data.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"Terrain({Name}) has an invalid terrain. This is not allowed.");
                return;
            }

            if (m_Data.Asset == null)
            {
                AsyncOperationHandle<UnityEngine.TerrainData> asyncHandle = m_Data.LoadAssetAsync();
                if (!asyncHandle.IsValid())
                {
                    CoreSystem.Logger.LogError(LogChannel.Entity,
                        $"Terrain({Name}) raised unexpected error.");
                    return;
                }

                asyncHandle.Completed += LoadTerrainDataAsync;

                if (onComplete != null) asyncHandle.Completed += ExecuteOnComplete;
            }
            else LoadTerrainData(m_Data.Asset);

            void ExecuteOnComplete(AsyncOperationHandle<UnityEngine.TerrainData> obj)
            {
                onComplete?.Invoke(m_TerrainInstance);
            }
        }

        private void LoadTerrainDataAsync(AsyncOperationHandle<UnityEngine.TerrainData> obj)
        {
#if DEBUG_MODE
            if (m_Material.IsNone() ||
                !m_Material.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                       $"Terrain({Name}) raised unexpected error. Material is not valid.");
                return;
            }
#endif
            if (m_Material.Asset == null)
            {
                var asyncHandle = m_Material.LoadAssetUntypedAsync();
#if DEBUG_MODE
                if (!asyncHandle.IsValid())
                {
                    CoreSystem.Logger.LogError(LogChannel.Entity,
                        $"Terrain({Name}) raised unexpected error. Material is not valid.");
                    return;
                }
#endif
                asyncHandle.Completed += LoadMaterialAsync;
            }
            else LoadTerrainData(obj.Result);
        }
        private void LoadMaterialAsync(AsyncOperationHandle obj)
        {
            LoadTerrainData(m_Data.Asset);
        }

        private void LoadTerrainData(UnityEngine.TerrainData obj)
        {
            GameObject terrainObj = Terrain.CreateTerrainGameObject(obj);
            terrainObj.layer = LevelDesignSystem.TerrainLayer;
            Terrain terrain = terrainObj.GetComponent<Terrain>();

            Transform tr = terrainObj.transform;
            TRS trs = TRS;
            tr.position = trs.m_Position;
            tr.rotation = trs.m_Rotation;
            tr.localScale = trs.m_Scale;

            terrain.materialTemplate = m_Material.Asset;

#if UNITY_EDITOR
            const string c_TerrainName = "Terrain_({0}, {1}, {2})";
            terrainObj.name = string.Format(c_TerrainName, trs.m_Position.x, trs.m_Position.y, trs.m_Position.z);
#endif
            m_TerrainInstance = terrain;

            if (m_EnableObstacle) m_NavMeshSystem.AddTerrain(this, m_AreaMask);
        }
    }
}
