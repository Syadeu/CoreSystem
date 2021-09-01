using Newtonsoft.Json.Utilities;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [DisplayName("Entity: UI Object Entity")]
    public sealed class UIObjectEntity : EntityBase
    {
        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<UIObjectEntity>>();
            AotHelper.EnsureList<Reference<UIObjectEntity>>();
            AotHelper.EnsureType<Entity<UIObjectEntity>>();
            AotHelper.EnsureList<Entity<UIObjectEntity>>();
            AotHelper.EnsureType<EntityData<UIObjectEntity>>();
            AotHelper.EnsureList<EntityData<UIObjectEntity>>();
            AotHelper.EnsureType<UIObjectEntity>();
            AotHelper.EnsureList<UIObjectEntity>();
        }
    }
}
