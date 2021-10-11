using BehaviorDesigner.Runtime;
using Syadeu.Presentation.Proxy;
using System;

namespace Syadeu.Presentation.BehaviorTree
{
    [Obsolete]
    public sealed class SharedRecycleableMonobehaviour : SharedVariable<RecycleableMonobehaviour>
    {
        public static implicit operator SharedRecycleableMonobehaviour(RecycleableMonobehaviour t)
        {
            return new SharedRecycleableMonobehaviour { Value = t };
        }

        public override string ToString() => Value.entity.Name;
    }
}
