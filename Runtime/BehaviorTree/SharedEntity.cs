using BehaviorDesigner.Runtime;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.BehaviorTree
{
    public sealed class SharedEntity : SharedVariable<Entity<IEntity>>
    {
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
