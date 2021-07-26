using Newtonsoft.Json;
using Syadeu.Mono;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class SceneDataEntity : EntityDataBase
    {
        [Tooltip("SceneIndex 의 씬이 로드될때 자동으로 데이터를 생성하나요?")]
        [JsonProperty(Order = 0, PropertyName = "BindScene")] public bool m_BindScene;
        [Tooltip("SceneList.Scenes 의 Index")]
        [JsonProperty(Order = 1, PropertyName = "SceneIndex")] public int m_SceneIndex;
        [JsonProperty(Order = 2, PropertyName = "MapData")] public Reference<MapDataEntity>[] m_MapData;

        public override bool IsValid()
        {
            if (m_BindScene)
            {
                if (m_SceneIndex < 0 || SceneList.Instance.Scenes.Count <= m_SceneIndex ||
                    m_MapData == null || m_MapData.Length == 0) return false;
            }
            return true;
        }

        public SceneReference GetTargetScene()
        {
            if (m_SceneIndex < 0 || SceneList.Instance.Scenes.Count <= m_SceneIndex) return null;
            return SceneList.Instance.Scenes[m_SceneIndex];
        }
    }
}
