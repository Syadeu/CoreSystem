using BehaviorDesigner.Runtime;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.BehaviorTree
{
    public sealed class SharedEntityList : SharedVariable<Entity<IEntity>[]>
    {
        public void SetValue(Entity<IEntity>[] entity)
        {
            base.SetValue(entity);
        }

        public static implicit operator SharedEntityList(Entity<IEntity>[] t)
        {
            return new SharedEntityList { Value = t };
        }
        public static implicit operator Entity<IEntity>[](SharedEntityList entity)
        {
            return entity.Value;
        }
    }
}
