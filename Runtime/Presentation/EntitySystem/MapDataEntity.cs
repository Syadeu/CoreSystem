using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.ThreadSafe;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation
{
    [EntityAcceptOnly(typeof(MapDataAttributeBase))]
    public sealed class MapDataEntity : EntityDataBase
    {
        public class Object
        {
            [JsonProperty(Order = 0, PropertyName = "Object")] public Reference<EntityBase> m_Object;
            [JsonProperty(Order = 1, PropertyName = "Translation")] public float3 m_Translation;
            [JsonProperty(Order = 2, PropertyName = "Rotation")] public quaternion m_Rotation;
            [JsonProperty(Order = 3, PropertyName = "Scale")] public float3 m_Scale;

            public Object()
            {
                m_Rotation = quaternion.identity;
            }
        }

        [JsonProperty(Order = 0, PropertyName = "Objects")] public Object[] m_Objects;

        public override bool IsValid()
        {
            return true;

        }
    }
    public sealed class MapDataProcessor : EntityDataProcessor<MapDataEntity>
    {
        protected override void OnCreated(MapDataEntity entity)
        {
            "entity in".ToLog();
            //CreateEntity(entity.m_Object, Vector3.Zero, quaternion.identity);
        }
    }
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

    [AttributeAcceptOnly(typeof(MapDataEntity))]
    public abstract class MapDataAttributeBase : AttributeBase { }

    public sealed class MapGridAttribute : MapDataAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Center")] public int3 m_Center;
        [JsonProperty(Order = 1, PropertyName = "Size")] public int3 m_Size;
    }
}
