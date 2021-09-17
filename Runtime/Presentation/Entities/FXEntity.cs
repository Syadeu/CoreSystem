using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Presentation.Proxy;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [DisplayName("Entity: FX Entity")]
    public sealed class FXEntity : EntityBase
    {
        //[JsonProperty(Order = 0, PropertyName = "HitFX")]
        //private Reference<FXEntity> m_HitFX = Reference<FXEntity>.Empty;


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
        IEntityOnProxyCreated, IEntityOnProxyRemoved
    {
        protected override void OnCreated(EntityData<FXEntity> entity)
        {
            ((ProxyTransform)entity.As().transform).enableCull = false;
        }
        public void OnProxyCreated(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //monoObj.gameObject.SetActive(false);
            ParticleSystem particle = monoObj.GetComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particle.main;
            main.playOnAwake = false;
        }
        public void OnProxyRemoved(EntityBase entityBase, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            //monoObj.gameObject.SetActive(false);
        }
    }
}
