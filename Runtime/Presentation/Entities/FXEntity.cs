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
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [DisplayName("Entity: FX Entity")]
    public sealed class FXEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "PlayOptions")]
        private FXBounds.PlayOptions m_PlayOptions = FXBounds.PlayOptions.OneShot;

        [JsonIgnore] private ParticleSystem m_ParticleSystem;
        [JsonIgnore] internal bool m_PlayQueued = false;
        [JsonIgnore] internal bool m_Stopped = false;

        [JsonIgnore] public bool IsPlaying
        {
            get
            {
                if (m_ParticleSystem == null)
                {

                    if (m_PlayQueued) return true;
                    return false;
                }
                return m_ParticleSystem.isPlaying;
            }
        }
        [JsonIgnore] public bool Stopped => m_Stopped;
        [JsonIgnore] public FXBounds.PlayOptions PlayOptions => m_PlayOptions;

        protected override void OnReserve()
        {
            m_PlayQueued = false;
            m_Stopped = false;
        }
        public void Play()
        {
            m_PlayQueued = true;
            if (m_ParticleSystem == null)
            {
                return;
            }
            Setup(m_ParticleSystem);

            m_ParticleSystem.Play();

            Render.IPlayable[] playables = m_ParticleSystem.GetComponentsInChildren<Render.IPlayable>();
            for (int i = 0; i < playables?.Length; i++)
            {
                playables[i].Play();
            }
            m_Stopped = false;
        }
        public void Stop()
        {
            if (m_ParticleSystem == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity, "");
                return;
            }

            m_PlayQueued = false;
            m_ParticleSystem.Stop();
            m_Stopped = true;
        }

        internal void Setup(ParticleSystem particleSystem)
        {
            m_ParticleSystem = particleSystem;
            if (m_ParticleSystem == null) return;

            m_ParticleSystem.Stop();

            ParticleSystem.MainModule main = m_ParticleSystem.main;
            main.playOnAwake = false;

            if ((m_PlayOptions & FXBounds.PlayOptions.Loop) == FXBounds.PlayOptions.Loop)
            {
                main.loop = true;
            }
            else if ((m_PlayOptions & FXBounds.PlayOptions.OneShot) == FXBounds.PlayOptions.OneShot)
            {
                main.loop = false;
            }
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<FXEntity>>();
            AotHelper.EnsureList<Reference<FXEntity>>();
            AotHelper.EnsureType<Entity<FXEntity>>();
            AotHelper.EnsureList<Entity<FXEntity>>();
            AotHelper.EnsureType<FXEntity>();
            AotHelper.EnsureList<FXEntity>();
        }
    }
    internal sealed class FXEntityProcessor : EntityProcessor<FXEntity>,
        IEntityOnProxyCreated, IEntityOnProxyRemoved
    {
        protected override void OnCreated(FXEntity entity)
        {
            entity.GetTransform().enableCull = false;
        }
        //protected override void OnDestroy(FXEntity entity)
        //{
        //    if (entity.transform.hasProxy)
        //    {
        //        RecycleableMonobehaviour monoObj = (RecycleableMonobehaviour)entity.transform.proxy;
        //        monoObj.RemoveOnParticleStoppedEvent(OnParticleStopped);
        //    }
        //}
        public void OnProxyCreated(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            FXEntity fx = (FXEntity)entityBase;
            var particle = monoObj.GetComponent<ParticleSystem>();
            var main = particle.main;
            main.stopAction = ParticleSystemStopAction.Callback;

            monoObj.AddOnParticleStoppedEvent(OnParticleStopped);

            fx.Setup(particle);

            if (fx.m_PlayQueued)
            {
                fx.m_PlayQueued = false;
                fx.Play();
            }
        }
        public void OnProxyRemoved(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //monoObj.gameObject.SetActive(false);
            monoObj.RemoveOnParticleStoppedEvent(OnParticleStopped);
        }

        private static void OnParticleStopped(Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            if (!entity.IsValid()) return;

            FXEntity fx = (FXEntity)entity.Target;
            fx.m_Stopped = true;
        }
    }
}
