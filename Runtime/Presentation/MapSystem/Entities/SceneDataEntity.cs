﻿using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [EntityAcceptOnly(typeof(SceneDataAttributeBase))]
    public sealed class SceneDataEntity : EntityDataBase
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [Tooltip("SceneIndex 의 씬이 로드될때 자동으로 데이터를 생성하나요?")]
        [JsonProperty(Order = 0, PropertyName = "BindScene")] internal bool m_BindScene;
        [Tooltip("SceneList.Scenes 의 Index")]
        [JsonProperty(Order = 1, PropertyName = "SceneIndex")] private int m_SceneIndex;
        [JsonProperty(Order = 2, PropertyName = "MapData")] private Reference<MapDataEntity>[] m_MapData;
#pragma warning restore IDE0044 // Add readonly modifier

        [JsonIgnore] public bool IsMapDataCreated { get; private set; } = false;
        [JsonIgnore] public IReadOnlyList<Reference<MapDataEntity>> MapData => m_MapData;
        [JsonIgnore] public List<EntityData<MapDataEntity>> CreatedMapData { get; } = new List<EntityData<MapDataEntity>>();

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

            for (int i = 0; i < m_MapData.Length; i++)
            {
                EntityData<IEntityData> temp = entitySystem.CreateObject(m_MapData[i]);
                CreatedMapData.Add(EntityData<MapDataEntity>.GetEntityData(temp.Idx));
            }

            IsMapDataCreated = true;
        }
        public void DestroyMapData()
        {
            if (!IsMapDataCreated) throw new System.Exception();

            for (int i = 0; i < CreatedMapData.Count; i++)
            {
                MapDataEntity mapData = CreatedMapData[i];
                mapData.DestroyChildOnDestroy = DestroyChildOnDestroy;
                CreatedMapData[i].Destroy();
            }

            CreatedMapData.Clear();
            IsMapDataCreated = false;
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
