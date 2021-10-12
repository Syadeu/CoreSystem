using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
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
        public enum PlayType
        {
            Sequence,
            Random
        }

        [JsonProperty(Order = 0, PropertyName = "PlayOptions")]
        private FXBounds.PlayOptions m_PlayOptions = FXBounds.PlayOptions.OneShot;
        //[JsonProperty(Order = 8, PropertyName = "FXBounds")]
        //protected PlayType m_PlayType = PlayType.Sequence;

        //[JsonProperty(Order = 0, PropertyName = "HitFX")]
        //private Reference<FXEntity> m_HitFX = Reference<FXEntity>.Empty;
        [JsonIgnore] private ParticleSystem m_ParticleSystem;
        [JsonIgnore] internal bool m_PlayQueued = false;

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

        public void SetPlayOptions(FXBounds.PlayOptions playOptions)
        {
            m_PlayOptions = playOptions;
            Setup(m_ParticleSystem);
        }
        public void Play()
        {
            if (m_ParticleSystem == null)
            {
                m_PlayQueued = true;
                return;
            }

            m_ParticleSystem.Play();
            Render.IPlayable[] playables = m_ParticleSystem.GetComponentsInChildren<Render.IPlayable>();
            for (int i = 0; i < playables?.Length; i++)
            {
                playables[i].Play();
            }
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
            AotHelper.EnsureType<Instance<FXEntity>>();
            AotHelper.EnsureList<Instance<FXEntity>>();
            AotHelper.EnsureType<InstanceArray<FXEntity>>();

            AotHelper.EnsureType<Reference<FXEntity>>();
            AotHelper.EnsureList<Reference<FXEntity>>();
            AotHelper.EnsureType<Entity<FXEntity>>();
            AotHelper.EnsureList<Entity<FXEntity>>();
            AotHelper.EnsureType<EntityData<FXEntity>>();
            AotHelper.EnsureList<EntityData<FXEntity>>();
            AotHelper.EnsureType<FXEntity>();
            AotHelper.EnsureList<FXEntity>();
        }
    }
    internal sealed class FXEntityProcessor : EntityDataProcessor<FXEntity>,
        IEntityOnProxyCreated
    {
        protected override void OnCreated(FXEntity entity)
        {
            ((ProxyTransform)entity.transform).enableCull = false;
        }
        public void OnProxyCreated(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            FXEntity fx = (FXEntity)entityBase;
            fx.Setup(monoObj.GetComponent<ParticleSystem>());

            if (fx.m_PlayQueued)
            {
                fx.m_PlayQueued = false;
                fx.Play();
            }
        }
        public void OnProxyRemoved(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //monoObj.gameObject.SetActive(false);
        }
    }
}
