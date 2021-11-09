#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TRPGTurnCountUIComponent : MonoBehaviour
    {
        [SerializeField] private string m_TextFormat = "TURN {0}\nPLACE";

        private TextMeshProUGUI m_Text;
        private TRPGTurnTableSystem m_TurnTableSystem;

        private IEnumerator Start()
        {
            m_Text = GetComponent<TextMeshProUGUI>();

            yield return PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.GetAwaiter();

            m_TurnTableSystem = PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.System;
            m_TurnTableSystem.OnStartTurn += System_OnStartTurn;
        }

        private void System_OnStartTurn(EntityData<IEntityData> obj)
        {
            m_Text.text = string.Format(m_TextFormat, m_TurnTableSystem.TurnCount);
        }
    }
}