using BehaviorDesigner.Runtime;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.BehaviorTree
{
    public sealed class SharedEntity : SharedVariable<Entity<IEntity>>
    {
        public void SetValue(Entity<IEntity> entity)
        {
            base.SetValue(entity);
        }

        public static implicit operator SharedEntity(Entity<IEntity> t)
        {
            return new SharedEntity { Value = t };
        }
        public static implicit operator Entity<IEntity>(SharedEntity entity)
        {
            return entity.Value;
        }
    }
}
