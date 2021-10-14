using Newtonsoft.Json;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Render;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("ParamAction: TRPG Camera Movement")]
    public sealed class TRPGCameraMovementParamAction : ParamAction<InputAction.CallbackContext>
    {
        [JsonProperty] public float MoveOffset { get; set; } = .07f;
        [JsonProperty] public float MoveSpeed { get; set; } = 8;

        protected override void OnExecute(InputAction.CallbackContext target)
        {
            CameraComponent m_Camera = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System.CameraComponent;
            if (m_Camera == null) return;

            TRPGCameraMovement m_Movement = m_Camera.GetCameraComponent<TRPGCameraMovement>();

            m_Movement.MoveOffset = MoveOffset;
            m_Movement.MoveSpeed = MoveSpeed;

            m_Movement.AxisVelocity = target.ReadValue<Vector2>();
        }
    }
}
