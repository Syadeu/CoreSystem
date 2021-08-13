using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Event;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class TriggerBoundAttribute : AttributeBase
    {
        [JsonIgnore] internal ClusterID m_ClusterID = ClusterID.Empty;

        [JsonProperty(Order = 0, PropertyName = "MatchWithAABB")] public bool m_MatchWithAABB;

        [Tooltip("만약 MatchWithAABB가 true일 경우, 아래 설정은 무시됩니다")]
        [JsonProperty(Order = 1, PropertyName = "Center")] public float3 m_Center = 0;
        [JsonProperty(Order = 2, PropertyName = "Size")] public float3 m_Size = 1;
    }
}
