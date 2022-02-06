#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Grid/TRPG/Draw UI Cell For Current Turn")]
    [Guid("D0E18103-0E86-4ECC-BAC2-847EEDD343F4")]
    internal sealed class TRPGDrawUICellConstAction : ConstAction<int>
    {
        [JsonProperty(Order = 0, PropertyName = "Enable")]
        public bool m_Enable = false;

        protected override int Execute()
        {
            if (!PresentationSystem<TRPGIngameSystemGroup, TRPGGridSystem>.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"Not ingame");

                return 0;
            }

            var gridSystem = PresentationSystem<TRPGIngameSystemGroup, TRPGGridSystem>.System;
            if (m_Enable)
            {
                var turntable = PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.System;

                gridSystem.DrawUICell(turntable.CurrentTurn);
                return 0;
            }

            gridSystem.ClearUICell();

            return 0;
        }
    }
}