using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Mono;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [DisplayName("EntityData: SceneData")]
    [EntityAcceptOnly(typeof(SceneDataAttributeBase))]
    public sealed class SceneDataEntity : EntityDataBase,
        INotifyComponent<SceneDataComponent>
    {
        [JsonProperty(Order = 0, PropertyName = "TerrainData")]
        internal Reference<TerrainData>[] m_TerrainData = Array.Empty<Reference<TerrainData>>();
        
        [Space]
#pragma warning disable IDE0044 // Add readonly modifier
        [Tooltip("SceneIndex 의 씬이 로드될때 자동으로 데이터를 생성하나요?")]
        [JsonProperty(Order = 1, PropertyName = "BindScene")] internal bool m_BindScene;
        [Tooltip("SceneList.Scenes 의 Index")]
        [JsonProperty(Order = 2, PropertyName = "SceneIndex")] private int m_SceneIndex;
        [JsonProperty(Order = 3, PropertyName = "MapData")] private Reference<MapDataEntity>[] m_MapData = Array.Empty<Reference<MapDataEntity>>();
#pragma warning restore IDE0044 // Add readonly modifier

        [JsonIgnore] internal InstanceArray<TerrainData> m_CreatedTerrains;

        [JsonIgnore] EntityData<IEntityData> INotifyComponent.Parent => EntityData<IEntityData>.GetEntityWithoutCheck(Idx);

        [JsonIgnore] public bool IsMapDataCreated { get; private set; } = false;
        [JsonIgnore] public IReadOnlyList<Reference<MapDataEntity>> MapData => m_MapData;
        [JsonIgnore] public EntityData<MapDataEntity>[] CreatedMapData { get; private set; }

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

            CreatedMapData = new EntityData<MapDataEntity>[m_MapData.Length];
            for (int i = 0; i < m_MapData.Length; i++)
            {
                if (m_MapData[i].IsEmpty() || !m_MapData[i].IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"MapData(Element At {i}) in SceneData({Name}) is not valid.");

                    CreatedMapData[i] = EntityData<MapDataEntity>.Empty;
                    continue;
                }

                EntityData<IEntityData> temp = entitySystem.CreateObject(m_MapData[i]);
                CreatedMapData[i] = temp.Cast<IEntityData, MapDataEntity>();
            }

            IsMapDataCreated = true;
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
        protected override void OnCreated(SceneDataEntity entity)
        {
            //if (!entity.Target.IsValid()) return;

            //entity.Target.CreateMapData(EntitySystem);
            //SceneDataEntity sceneData = entity.Target;

            //sceneData.m_CreatedTerrains = new InstanceArray<TerrainData>(sceneData.m_TerrainData, Allocator.Persistent);
            //for (int i = 0; i < sceneData.m_CreatedTerrains.Length; i++)
            //{
            //    sceneData.m_CreatedTerrains[i].Object.Create(null);
            //}

            CreateMapData(entity);
        }
        private void CreateMapData(SceneDataEntity sceneDataEntity)
        {
            IReadOnlyList<Reference<MapDataEntity>> mapData = sceneDataEntity.MapData;

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(sceneDataEntity.Idx);
            entity.AddComponent<SceneDataComponent>();

            ref SceneDataComponent sceneData = ref entity.GetComponent<SceneDataComponent>();
            sceneData.m_Created = true;

            //sceneData.m_CreatedMapData = new InstanceArray<MapDataEntity>(mapData.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            sceneData.m_CreatedMapData = new FixedInstanceList64<MapDataEntity>();
            for (int i = 0; i < mapData.Count; i++)
            {
                if (mapData[i].IsEmpty() || !mapData[i].IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"MapData(Element At {i}) in SceneData({sceneDataEntity.Name}) is not valid.");

                    //sceneData.m_CreatedMapData.Add(Instance<MapDataEntity>.Empty);
                    continue;
                }

                EntityData<IEntityData> temp = EntitySystem.CreateObject(mapData[i]);
                sceneData.m_CreatedMapData.Add(new Instance<MapDataEntity>(temp));
            }

            //sceneData.m_CreatedTerrains = new InstanceArray<TerrainData>(sceneDataEntity.m_TerrainData, Allocator.Persistent);
            sceneData.m_CreatedTerrains = new FixedInstanceList64<TerrainData>();
            for (int i = 0; i < sceneDataEntity.m_TerrainData.Length; i++)
            {
                if (sceneDataEntity.m_TerrainData[i].IsEmpty() ||
                    !sceneDataEntity.m_TerrainData[i].IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"TerrainData(Element At {i}) in SceneData({sceneDataEntity.Name}) is not valid.");

                    continue;
                }

                Instance<TerrainData> temp = EntitySystem.CreateInstance(sceneDataEntity.m_TerrainData[i]);
                sceneData.m_CreatedTerrains.Add(temp);
            }

            for (int i = 0; i < sceneData.m_CreatedTerrains.Length; i++)
            {
                sceneData.m_CreatedTerrains[i].GetObject().Create(null);
            }
        }
        protected override void OnDestroy(SceneDataEntity entity)
        {
            //if (entity.Target == null || !entity.Target.IsValid()) return;

            //entity.Target.DestroyMapData();
            //SceneDataEntity sceneData = entity.Target;
            //for (int i = 0; i < sceneData.m_CreatedTerrains.Length; i++)
            //{
            //    sceneData.m_CreatedTerrains[i].Destroy();
            //}
            //sceneData.m_CreatedTerrains.Dispose();
        }
    }
}
