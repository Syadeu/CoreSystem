#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGAddAPAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "AddActionPoint")]
        private int m_AddActionPoint = 0;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
#if DEBUG_MODE
            if (!entity.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"");
                return;
            }
#endif
            ref TurnPlayerComponent turnPlayer = ref entity.GetComponent<TurnPlayerComponent>();
            
            if (turnPlayer.ActionPoint + m_AddActionPoint < 0)
            {
                turnPlayer.ActionPoint = 0;
            }
            else turnPlayer.ActionPoint += m_AddActionPoint;
        }
    }
}