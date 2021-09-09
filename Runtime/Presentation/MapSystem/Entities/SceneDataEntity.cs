using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [DisplayName("EntityData: SceneData")]
    [EntityAcceptOnly(typeof(SceneDataAttributeBase))]
    public sealed class SceneDataEntity : EntityDataBase
    {
        [JsonProperty(Order = 0, PropertyName = "TerrainData")]
        private Reference<TerrainData> m_TerrainData = Reference<TerrainData>.Empty;
        
        [Space]
#pragma warning disable IDE0044 // Add readonly modifier
        [Tooltip("SceneIndex 의 씬이 로드될때 자동으로 데이터를 생성하나요?")]
        [JsonProperty(Order = 1, PropertyName = "BindScene")] internal bool m_BindScene;
        [Tooltip("SceneList.Scenes 의 Index")]
        [JsonProperty(Order = 2, PropertyName = "SceneIndex")] private int m_SceneIndex;
        [JsonProperty(Order = 3, PropertyName = "MapData")] private Reference<MapDataEntity>[] m_MapData = Array.Empty<Reference<MapDataEntity>>();
#pragma warning restore IDE0044 // Add readonly modifier

        [JsonIgnore] private readonly List<Terrain> m_CreatedTerrains = new List<Terrain>();

        [JsonIgnore] public bool IsMapDataCreated { get; private set; } = false;
        [JsonIgnore] public IReadOnlyList<Reference<MapDataEntity>> MapData => m_MapData;
        [JsonIgnore] public EntityData<MapDataEntity>[] CreatedMapData { get; private set; }
        [JsonIgnore] public IReadOnlyList<Terrain> CreatedTerrains => m_CreatedTerrains;

        [JsonIgnore] public bool DestroyChildOnDestroy { get; set; } = true;

        public override bool IsValid()
        {
            if (m_BindScene)
            {
                if (m_SceneIndex < 0 || SceneList.Instance.Scenes.Count <= m_SceneIndex) return false;
            }
            if (m_MapData == null || m_MapData.Length == 0) return false;
            return true;
        }

        public SceneReference GetTargetScene()
        {
            if (m_SceneIndex < 0 || SceneList.Instance.Scenes.Count <= m_SceneIndex) return null;
            return SceneList.Instance.Scenes[m_SceneIndex];
        }

        public void CreateMapData(EntitySystem entitySystem)
        {
            if (IsMapDataCreated) throw new System.Exception();

            if (m_TerrainData.IsValid())
            {
                TerrainData terrainData = m_TerrainData.GetObject();
                for (int i = 0; i < terrainData.m_Data.Length; i++)
                {
                    if (!terrainData.m_Data[i].IsValid() || terrainData.m_Data[i].IsNone()) continue;

                    if (terrainData.m_Data[i].Asset == null)
                    {
                        AsyncOperationHandle<UnityEngine.TerrainData> asyncHandle = terrainData.m_Data[i].LoadAssetAsync();
                        if (!asyncHandle.IsValid())
                        {
                            CoreSystem.Logger.LogError(Channel.Entity,
                                $"Terrain Data({terrainData.Name}) has invalid data element at {terrainData.m_Data[i].GetObjectSetting().m_Name}({i}).");
                            continue;
                        }

                        asyncHandle.Completed += LoadTerrainDataAsync;
                    }
                    else LoadTerrainData(terrainData.m_Data[i].Asset);
                }
            }

            CreatedMapData = new EntityData<MapDataEntity>[m_MapData.Length];
            for (int i = 0; i < m_MapData.Length; i++)
            {
                EntityData<IEntityData> temp = entitySystem.CreateObject(m_MapData[i]);
                CreatedMapData[i] = EntityData<MapDataEntity>.GetEntity(temp.Idx);
            }

            IsMapDataCreated = true;
        }

        private void LoadTerrainDataAsync(AsyncOperationHandle<UnityEngine.TerrainData> obj)
        {
            LoadTerrainData(obj.Result);
        }
        private void LoadTerrainData(UnityEngine.TerrainData obj)
        {
            GameObject terrainObj = Terrain.CreateTerrainGameObject(obj);
            terrainObj.layer = LevelDesignSystem.TerrainLayer;
            Terrain terrain = terrainObj.GetComponent<Terrain>();

            m_CreatedTerrains.Add(terrain);
        }

        public void DestroyMapData()
        {
            if (!IsMapDataCreated) throw new System.Exception();

            for (int i = 0; i < CreatedMapData.Length; i++)
            {
                MapDataEntity mapData = CreatedMapData[i];
                mapData.DestroyChildOnDestroy = DestroyChildOnDestroy;
                CreatedMapData[i].Destroy();
            }

            for (int i = 0; i < m_CreatedTerrains.Count; i++)
            {
                UnityEngine.Object.Destroy(m_CreatedTerrains[i].gameObject);
            }
            m_CreatedTerrains.Clear();

            CreatedMapData = null;
            IsMapDataCreated = false;
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<SceneDataEntity>>();
            AotHelper.EnsureList<Reference<SceneDataEntity>>();
            AotHelper.EnsureType<EntityData<SceneDataEntity>>();
            AotHelper.EnsureList<EntityData<SceneDataEntity>>();
            AotHelper.EnsureType<SceneDataEntity>();
            AotHelper.EnsureList<SceneDataEntity>();
        }
    }
    [Preserve]
    internal sealed class SceneDataEntityProcessor : EntityDataProcessor<SceneDataEntity>
    {
        protected override void OnCreated(EntityData<SceneDataEntity> entity)
        {
            if (!entity.Target.IsValid()) return;

            entity.Target.CreateMapData(EntitySystem);
        }
        protected override void OnDestroy(EntityData<SceneDataEntity> entity)
        {
            if (entity.Target == null || !entity.Target.IsValid()) return;

            entity.Target.DestroyMapData();
        }
    }
}
