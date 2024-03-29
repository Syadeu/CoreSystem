﻿#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGHasAPPredicateAction : TriggerPredicateAction
    {
        [UnityEngine.SerializeField, JsonProperty(Order = 0, PropertyName = "DesireActionPoint")]
        private int m_DesireActionPoint = 0;

        protected override bool OnExecute(Entity<IObject> entity)
        {
            if (!entity.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(LogChannel.Entity,
                    $"");
                return false;
            }

            ref TurnPlayerComponent turnPlayer = ref entity.GetComponent<TurnPlayerComponent>();

            if (turnPlayer.ActionPoint >= m_DesireActionPoint) return true;
            return false;
        }
    }
}