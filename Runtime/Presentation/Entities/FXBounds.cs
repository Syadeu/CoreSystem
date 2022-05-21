﻿// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
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

            OnFire          =   FireOnSuccess | FireOnFailed,
            
            FireOnSuccess   =   0b0001,
            FireOnFailed    =   0b0010
        }
        [Flags]
        public enum PlayOptions
        {
            None            =   0,

            Loop            =   0b0001,
            OneShot         =   0b0010,

            UpdateTransform    =   0b0100,
        }

        [UnityEngine.SerializeField]
        [JsonProperty(Order = 0, PropertyName = "Name")]
        private string m_Name = string.Empty;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 1, PropertyName = "TriggerOption")]
        private TriggerOptions m_TriggerOption = TriggerOptions.OnFire;
        //[JsonProperty(Order = 2, PropertyName = "PlayOption")]
        //private PlayOptions m_PlayOption = PlayOptions.OneShot;

        [Space]
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 3, PropertyName = "FXEntity")]
        private Reference<FXEntity> m_FXEntity = Reference<FXEntity>.Empty;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 4, PropertyName = "LocalPosition")]
        private float3 m_LocalPosition;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 5, PropertyName = "LocalRotation")]
        private float3 m_LocalRotation;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 6, PropertyName = "LocalScale")]
        private float3 m_LocalScale = 1;

        [JsonIgnore] private Entity<FXEntity> m_Instance = Entity<FXEntity>.Empty;

        [JsonIgnore] public Reference<FXEntity> FXEntity => m_FXEntity;
        [JsonIgnore] public TriggerOptions TriggerOption => m_TriggerOption;
        [JsonIgnore] public TRS TRS => new TRS(m_LocalPosition, m_LocalRotation, m_LocalScale);

        public bool IsValid()
        {
            return m_FXEntity.IsValid();
        }

        public void Fire(CoroutineSystem coroutineSystem, ProxyTransform parent)
        {
            if (parent is ProxyTransform proxy)
            {
                m_Instance = m_FXEntity.CreateEntity();
                //FXEntity fx = m_Instance.Target;
                //fx.SetPlayOptions(m_PlayOption);

                //coroutineSystem.StartCoroutine(new FireCoroutine
                //{
                //    m_FXEntity = m_Instance,
                //    m_PlayOption = fx.PlayOptions,
                //    TRS = TRS,

                //    Parent = proxy
                //});

                m_Instance.transform.SetParent(parent);
            }
            else
            {
                CoreSystem.Logger.LogError(LogChannel.Entity, "");
            }
        }
        public void Stop()
        {
            if (m_Instance.IsEmpty() || !m_Instance.IsValid())
            {
                "".ToLogError();
                return;
            }

            m_Instance.Destroy();
            m_Instance = Entity<FXEntity>.Empty;
        }

        //private struct FireCoroutine : ICoroutineJob
        //{
        //    public Entity<FXEntity> m_FXEntity;
        //    public PlayOptions m_PlayOption;
        //    public TRS TRS;

        //    public ProxyTransform Parent;

        //    public UpdateLoop Loop => UpdateLoop.AfterTransform;

        //    public void Dispose()
        //    {
        //    }
        //    public IEnumerator Execute()
        //    {
        //        if (!m_FXEntity.IsValid())
        //        {
        //            CoreSystem.Logger.LogError(Channel.Entity,
        //                $"Null return");
        //            yield break;
        //        }

        //        //Instance<FXEntity> instance = m_FXEntity.CreateInstance();
        //        FXEntity fx = m_FXEntity.Target;
        //        //fx.SetPlayOptions(m_PlayOption);

        //        ITransform tr = fx.GetTransform();

        //        TRS trs = TRS.Project(new TRS(Parent));
        //        tr.position = trs.m_Position;
        //        tr.rotation = trs.m_Rotation;
        //        tr.scale = TRS.m_Scale;

        //        fx.Play();

        //        $"{m_FXEntity.Target.Name} fired".ToLog();

        //        while (fx.IsPlaying && !fx.Stopped)
        //        {
        //            if ((m_PlayOption & PlayOptions.UpdateTransform) == PlayOptions.UpdateTransform)
        //            {
        //                trs = TRS.Project(new TRS(Parent));
        //                tr.position = trs.m_Position;
        //                tr.rotation = trs.m_Rotation;
        //                tr.scale = TRS.m_Scale;
        //            }

        //            yield return null;
        //        }

        //        "destroy fx".ToLog();
        //        m_FXEntity.Destroy();
        //    }
        //}
    }
}
