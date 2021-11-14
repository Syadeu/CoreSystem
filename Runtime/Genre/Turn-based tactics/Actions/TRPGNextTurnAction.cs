#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.TurnTable.UI;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("TriggerAction: TRPG Next turn")]
    public sealed class TRPGNextTurnAction : TriggerAction
    {
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            PresentationSystem<DefaultPresentationGroup, EventSystem>
                .System
                .PostEvent(TRPGEndTurnUIPressedEvent.GetEvent());
        }
    }
}