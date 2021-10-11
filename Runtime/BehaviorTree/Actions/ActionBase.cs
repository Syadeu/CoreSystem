using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.BehaviorTree
{
    public abstract class ActionBase : Action
    {
        private SharedEntity m_This = null;

        public Entity<IEntity> Entity => m_This.Value;

        public override void OnStart()
        {
            m_This = (SharedEntity)Owner.GetVariable(PresentationBehaviorTreeUtility.c_SelfEntityString);

            if (m_This == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"BehaviorTree Entity not found error.");
            }
        }
    }
}
