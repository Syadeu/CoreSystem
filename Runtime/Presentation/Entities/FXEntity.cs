using Newtonsoft.Json.Utilities;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [DisplayName("Entity: FX Entity")]
    public sealed class FXEntity : EntityBase
    {
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
}
