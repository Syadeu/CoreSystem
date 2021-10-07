using Newtonsoft.Json;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Overlay UI Entry")]
    public sealed class ActorOverlayUIEntry : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "CreateOnStart")]
        public bool m_CreateOnStart = false;

        [JsonProperty(Order = 1, PropertyName = "Prefab")]
        public Reference<UIObjectEntity> m_Prefab = Reference<UIObjectEntity>.Empty;
        [JsonProperty(Order = 2, PropertyName = "UpdateType")]
        public UpdateType m_UpdateType = UpdateType.Instant | UpdateType.SyncCameraOrientation;
        [JsonProperty(Order = 3, PropertyName = "UpdateSpeed")]
        public float m_UpdateSpeed = 4;

        [Space, Header("Position")]
        [JsonProperty(Order = 4, PropertyName = "Offset")]
        public float3 m_Offset = float3.zero;

        [Space, Header("Orientation")]
        [JsonProperty(Order = 5, PropertyName = "OrientationOffset")]
        public float3 m_OrientationOffset = float3.zero;
    }
}
