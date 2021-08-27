using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actions
{
    public sealed class DestroyEntityAction : TriggerAction<DestroyEntityAction>
    {
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            entity.Destroy();
        }
    }
}
