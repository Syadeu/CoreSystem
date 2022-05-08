#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Render;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Camera/TRPG/Set Camera Inventory")]
    [Guid("8564BD97-3C87-40B4-B464-1256AFEA45E9")]
    internal sealed class TRPGSetCameraInventoryConstAction : ConstTriggerAction<int>
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

        protected override int Execute(InstanceID entity)
        {
            if (m_TRPGCameraMovement == null)
            {
                m_TRPGCameraMovement = m_RenderSystem.CameraComponent.GetCameraComponent<TRPGCameraMovement>();
            }

            ActorControllerComponent ctr = entity.GetComponent<ActorControllerComponent>();
            if (!entity.HasComponent<TRPGActorAttackComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.GetEntity().Target.Name}) doesn\'t have {nameof(TRPGActorAttackComponent)}.");

                return 0;
            }

            var tr = entity.GetTransform();
            m_TRPGCameraMovement.SetInventory(tr);

            return 0;
        }
    }
}