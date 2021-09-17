using Newtonsoft.Json;
using Syadeu.Presentation.Proxy;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Entities
{
    [Serializable]
    public sealed class FXBounds
    {
        [Flags]
        public enum TriggerOptions
        {
            None        =   0,

            OnFire      =   0b001,
        }

        [JsonProperty(Order = 0, PropertyName = "Name")]
        private string m_Name = string.Empty;
        [JsonProperty(Order = 1, PropertyName = "TriggerOption")]
        private TriggerOptions m_TriggerOption = TriggerOptions.OnFire;

        [Space]
        [JsonProperty(Order = 2, PropertyName = "FXEntity")]
        private Reference<FXEntity> m_FXEntity = Reference<FXEntity>.Empty;
        [JsonProperty(Order = 3, PropertyName = "LocalPosition")]
        private float3 m_LocalPosition;
        [JsonProperty(Order = 4, PropertyName = "LocalRotation")]
        private float3 m_LocalRotation;
        [JsonProperty(Order = 5, PropertyName = "LocalScale")]
        private float3 m_LocalScale = 1;

        [JsonIgnore] public Reference<FXEntity> FXEntity => m_FXEntity;
        [JsonIgnore] public TriggerOptions TriggerOption => m_TriggerOption;
        [JsonIgnore] public TRS TRS => new TRS(m_LocalPosition, m_LocalRotation, m_LocalScale);
    }
}
