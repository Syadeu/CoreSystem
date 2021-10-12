using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Destroy Entity")]
    public sealed class DestroyEntityAction : TriggerAction
    {
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            entity.Destroy();
        }
    }
}
