#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Camera/TRPG/Set Camera Aim Target")]
    [Guid("5416C26F-4188-4FE1-94AC-874BDEB4363E")]
    internal sealed class TRPGSetCameraAimTargetConstAction : ConstAction<int>
    {
        [JsonIgnore]
        private RenderSystem m_RenderSystem;
        [JsonIgnore]
        private TRPGCameraMovement m_TRPGCameraMovement;
        [JsonIgnore]
        private TRPGInputSystem m_TRPGInputSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, RenderSystem>(Bind);
            RequestSystem<TRPGAppCommonSystemGroup, TRPGInputSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_RenderSystem = null;
            m_TRPGInputSystem = null;
        }

        private void Bind(RenderSystem other)
        {
            m_RenderSystem = other;
        }
        private void Bind(TRPGInputSystem other)
        {
            m_TRPGInputSystem = other;
        }

        protected override int Execute()
        {
            if (m_TRPGCameraMovement == null)
            {
                m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();
            }

            if (!PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"Is not ingame.");

                return 0;
            }

            TRPGTurnTableSystem system = PresentationSystem<TRPGIngameSystemGroup, TRPGTurnTableSystem>.System;
            ActorControllerComponent ctr = system.CurrentTurn.GetComponent<ActorControllerComponent>();
            if (!system.CurrentTurn.HasComponent<TRPGActorAttackComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({system.CurrentTurn.RawName}) doesn\'t have {nameof(TRPGActorAttackComponent)}.");

                return 0;
            }

            Entity<TRPGActorAttackProvider> attProvider = ctr.GetProvider<TRPGActorAttackProvider>();
            var targets = attProvider.Target.GetTargetsInRange();
            var tr = system.CurrentTurn.transform;

            $"{targets.Length} found".ToLog();
            m_TRPGInputSystem.SetIngame_TargetAim();

            if (targets.Length == 0) return 0;

            ref TRPGActorAttackComponent attackComponent = ref system.CurrentTurn.GetComponent<TRPGActorAttackComponent>();
            if (attackComponent.GetTarget().IsEmpty())
            {
                attackComponent.SetTarget(0);
            }

            m_TRPGCameraMovement.SetAim(tr, attackComponent.GetTarget().GetTransform());

            return 0;
        }
    }
}