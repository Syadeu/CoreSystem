using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Proxy;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Entities
{
    [Serializable]
    public sealed class FXBounds : IValidation
    {
        [Flags]
        public enum TriggerOptions
        {
            None        =   0,

            OnFire      =   0b001,
        }
        [Flags]
        public enum PlayOptions
        {
            None            =   0,

            Loop            =   0b0001,
            OneShot         =   0b0010,

            DestroyOnEnd    =   0b0100,
        }

        [JsonProperty(Order = 0, PropertyName = "Name")]
        private string m_Name = string.Empty;
        [JsonProperty(Order = 1, PropertyName = "TriggerOption")]
        private TriggerOptions m_TriggerOption = TriggerOptions.OnFire;
        [JsonProperty(Order = 2, PropertyName = "PlayOption")]
        private PlayOptions m_PlayOption = PlayOptions.OneShot;

        [Space]
        [JsonProperty(Order = 3, PropertyName = "FXEntity")]
        private Reference<FXEntity> m_FXEntity = Reference<FXEntity>.Empty;
        [JsonProperty(Order = 4, PropertyName = "LocalPosition")]
        private float3 m_LocalPosition;
        [JsonProperty(Order = 5, PropertyName = "LocalRotation")]
        private float3 m_LocalRotation;
        [JsonProperty(Order = 6, PropertyName = "LocalScale")]
        private float3 m_LocalScale = 1;

        [JsonIgnore] private Instance<FXEntity> m_Instance = Instance<FXEntity>.Empty;

        [JsonIgnore] public Reference<FXEntity> FXEntity => m_FXEntity;
        [JsonIgnore] public TriggerOptions TriggerOption => m_TriggerOption;
        [JsonIgnore] public TRS TRS => new TRS(m_LocalPosition, m_LocalRotation, m_LocalScale);

        public bool IsValid()
        {
            return ((IValidation)m_FXEntity).IsValid();
        }

        public void Fire(ITransform parent)
        {
            if (m_Instance.IsEmpty())
            {
                m_Instance = m_FXEntity.CreateInstance();
            }
            m_Instance.Object.SetPlayOptions(m_PlayOption);

            TRS trs = TRS.Project(new TRS(parent));
            ITransform tr = m_Instance.Object.transform;

            tr.position = trs.m_Position;
            tr.rotation = trs.m_Rotation;
            tr.scale = trs.m_Scale;

            m_Instance.Object.Play();

            $"{m_FXEntity.GetObject().Name} fired".ToLog();
        }
        public void DestroyInstance()
        {
            if (m_Instance.IsEmpty())
            {
                CoreSystem.Logger.LogError(Channel.Entity, "Already destroyed.");
                return;
            }

            m_Instance.Destroy();
            m_Instance = Instance<FXEntity>.Empty;
        }
    }
}
