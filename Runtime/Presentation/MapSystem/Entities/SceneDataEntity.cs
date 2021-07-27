using Newtonsoft.Json;
using Syadeu.Mono;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [EntityAcceptOnly(typeof(SceneDataAttributeBase))]
    public sealed class SceneDataEntity : EntityDataBase
    {
        [Tooltip("SceneIndex 의 씬이 로드될때 자동으로 데이터를 생성하나요?")]
        [JsonProperty(Order = 0, PropertyName = "BindScene")] public bool m_BindScene;
        [Tooltip("SceneList.Scenes 의 Index")]
        [JsonProperty(Order = 1, PropertyName = "SceneIndex")] public int m_SceneIndex;
        [JsonProperty(Order = 2, PropertyName = "MapData")] public Reference<MapDataEntity>[] m_MapData;

        [JsonIgnore] public readonly List<IObject> m_CreatedMapData = new List<IObject>();

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
    }
    [Preserve]
    internal sealed class SceneDataEntityProcessor : EntityDataProcessor<SceneDataEntity>
    {
        protected override void OnCreated(SceneDataEntity entity)
        {
            if (!entity.IsValid()) return;

            for (int i = 0; i < entity.m_MapData.Length; i++)
            {
                IObject temp = CreateObject(entity.m_MapData[i]);
                entity.m_CreatedMapData.Add(temp);
            }
        }
        protected override void OnDestroy(SceneDataEntity entity)
        {
            if (!entity.IsValid()) return;

            for (int i = 0; i < entity.m_CreatedMapData.Count; i++)
            {
                MapDataEntity mapData = (MapDataEntity)entity.m_CreatedMapData[i];
                mapData.DestroyChildOnDestroy = entity.DestroyChildOnDestroy;
                mapData.Destroy();
            }
        }
    }
}
