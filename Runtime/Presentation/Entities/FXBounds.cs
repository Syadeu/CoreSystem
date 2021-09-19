﻿using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections;
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
            None            =   0,

            OnFire          =   0b001,
            OnDelayedFire   =   0b010,
        }
        [Flags]
        public enum PlayOptions
        {
            None            =   0,

            Loop            =   0b0001,
            OneShot         =   0b0010,

            UpdateTransform    =   0b0100,
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

        //[JsonIgnore] private Instance<FXEntity> m_Instance = Instance<FXEntity>.Empty;

        [JsonIgnore] public Reference<FXEntity> FXEntity => m_FXEntity;
        [JsonIgnore] public TriggerOptions TriggerOption => m_TriggerOption;
        [JsonIgnore] public TRS TRS => new TRS(m_LocalPosition, m_LocalRotation, m_LocalScale);

        public bool IsValid()
        {
            return ((IValidation)m_FXEntity).IsValid();
        }

        public void Fire(PresentationSystemID<CoroutineSystem> coroutineSystem, ITransform parent)
        {
            if (parent is ProxyTransform proxy)
            {
                coroutineSystem.System.PostCoroutineJob(new FireCoroutine
                {
                    m_FXEntity = m_FXEntity,
                    m_PlayOption = m_PlayOption,
                    TRS = TRS,

                    Parent = proxy
                });
            }
            else
            {
                CoreSystem.Logger.LogError(Channel.Entity, "");
            }
        }

        private struct FireCoroutine : ICoroutineJob
        {
            public Reference<FXEntity> m_FXEntity;
            public PlayOptions m_PlayOption;
            public TRS TRS;

            public ProxyTransform Parent;

            public UpdateLoop Loop => UpdateLoop.AfterTransform;

            public void Dispose()
            {
            }
            public IEnumerator Execute()
            {
                if (!m_FXEntity.IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Null return");
                    yield break;
                }

                Instance<FXEntity> instance = m_FXEntity.CreateInstance();
                FXEntity fx = instance.Object;
                fx.SetPlayOptions(m_PlayOption);

                ITransform tr = fx.transform;

                TRS trs = TRS.Project(new TRS(Parent));
                tr.position = trs.m_Position;
                tr.rotation = trs.m_Rotation;
                tr.scale = TRS.m_Scale;

                fx.Play();

                $"{m_FXEntity.GetObject().Name} fired".ToLog();

                while (fx.IsPlaying)
                {
                    if ((m_PlayOption & PlayOptions.UpdateTransform) == PlayOptions.UpdateTransform)
                    {
                        trs = TRS.Project(new TRS(Parent));
                        tr.position = trs.m_Position;
                        tr.rotation = trs.m_Rotation;
                        tr.scale = TRS.m_Scale;
                    }

                    yield return null;
                }

                "exit".ToLog();
                instance.Destroy();
            }
        }
    }
}
