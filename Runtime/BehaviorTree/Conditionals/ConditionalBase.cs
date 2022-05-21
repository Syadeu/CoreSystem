using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.BehaviorTree
{
    public abstract class ConditionalBase : Conditional
    {
        private SharedEntity m_This = null;

        public Entity<IEntity> Entity => m_This.Value;

        public override void OnStart()
        {
            m_This = (SharedEntity)Owner.GetVariable(PresentationBehaviorTreeUtility.c_SelfEntityString);

            if (m_This == null)
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"BehaviorTree Entity not found error.");
            }
        }
    }
}
