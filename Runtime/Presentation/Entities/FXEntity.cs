using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [DisplayName("Entity: FX Entity")]
    public sealed class FXEntity : EntityBase
    {
        [JsonProperty(Order = 0, PropertyName = "HitFX")]
        private Reference<ParticleEntity> m_HitFX = Reference<ParticleEntity>.Empty;

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

    [DisplayName("Entity: Particle Entity")]
    public sealed class ParticleEntity : EntityBase
    {
        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Instance<ParticleEntity>>();
            AotHelper.EnsureList<Instance<ParticleEntity>>();
            AotHelper.EnsureType<InstanceArray<ParticleEntity>>();

            AotHelper.EnsureType<Reference<ParticleEntity>>();
            AotHelper.EnsureList<Reference<ParticleEntity>>();
            AotHelper.EnsureType<Entity<ParticleEntity>>();
            AotHelper.EnsureList<Entity<ParticleEntity>>();
            AotHelper.EnsureType<EntityData<ParticleEntity>>();
            AotHelper.EnsureList<EntityData<ParticleEntity>>();
            AotHelper.EnsureType<ParticleEntity>();
            AotHelper.EnsureList<ParticleEntity>();
        }
    }
}
