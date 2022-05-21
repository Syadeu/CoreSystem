#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Grid/TRPG/Draw UI Cell At Target")]
    [Guid("D0E18103-0E86-4ECC-BAC2-847EEDD343F4")]
    internal sealed class TRPGDrawUICellConstAction : ConstTriggerAction<int>
    {
        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "Enable")]
        public bool m_Enable = false;

        protected override int Execute(InstanceID entity)
        {
            if (!PresentationSystem<TRPGIngameSystemGroup, TRPGGridSystem>.IsValid())
            {
                CoreSystem.Logger.LogError(LogChannel.Action,
                    $"Not ingame");

                return 0;
            }

            var gridSystem = PresentationSystem<TRPGIngameSystemGroup, TRPGGridSystem>.System;
            if (m_Enable)
            {
                gridSystem.DrawUICell(entity);
                return 0;
            }

            gridSystem.ClearUICell();

            return 0;
        }
    }
}