using Newtonsoft.Json.Utilities;
using Syadeu.Presentation.Entities;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    public abstract class ActorEntityBase : EntityBase
    {
        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<ActorEntityBase>>();
            AotHelper.EnsureList<Reference<ActorEntityBase>>();
            AotHelper.EnsureList<Entity<ActorEntityBase>>();
            AotHelper.EnsureList<EntityData<ActorEntityBase>>();
            AotHelper.EnsureList<ActorEntityBase>();
        }
    }
}
