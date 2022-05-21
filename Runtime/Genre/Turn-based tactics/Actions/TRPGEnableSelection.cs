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
    [DisplayName("System/Enable Selection")]
    [Description("Entity 가 선택될 수 있는 지 전역 설정을 합니다.")]
    [Guid("C248E75D-AEA5-4B10-A98F-F0528080D39E")]
    internal sealed class TRPGEnableSelection : ConstAction<int>
    {
        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "Enable")]
        private bool m_Enable = false;

        protected override int Execute()
        {
            TRPGSelectionSystem selectionSystem = PresentationSystem<TRPGIngameSystemGroup, TRPGSelectionSystem>.System;
            if (selectionSystem == null)
            {
                "??".ToLogError();
                return 0;
            }

            selectionSystem.EnableSelection = m_Enable;

            return 0;
        }
    }
}