using BehaviorDesigner.Runtime;
using Syadeu.Collections;

namespace Syadeu.Presentation.BehaviorTree
{
    public sealed class SharedReference : SharedVariable<Reference>
    {
        public void SetValue(Reference entity)
        {
            base.SetValue(entity);
        }

        public static implicit operator SharedReference(Reference t)
        {
            return new SharedReference { Value = t };
        }
        public static implicit operator Reference(SharedReference entity)
        {
            return entity.Value;
        }
    }
    public sealed class SharedReference<T> : SharedVariable<Reference<T>>
        where T : class, IObject
    {
        public void SetValue(Reference<T> entity)
        {
            base.SetValue(entity);
        }

        public static implicit operator SharedReference<T>(Reference<T> t)
        {
            return new SharedReference<T> { Value = t };
        }
        public static implicit operator Reference<T>(SharedReference<T> entity)
        {
            return entity.Value;
        }
    }
}
