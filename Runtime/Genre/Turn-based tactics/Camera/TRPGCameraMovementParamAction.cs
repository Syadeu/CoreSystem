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

        [JsonIgnore] private CameraComponent m_Camera;
        [JsonIgnore] private TRPGCameraMovement m_Movement;

        protected override void OnCreated()
        {
            m_Camera = PresentationSystem<RenderSystem>.System.Camera.GetComponent<CameraComponent>();
            CoreSystem.Logger.NotNull(m_Camera, "Camera null");

            m_Movement = m_Camera.GetCameraComponent<TRPGCameraMovement>();

            CoreSystem.Logger.NotNull(m_Camera);
            CoreSystem.Logger.NotNull(m_Movement);
        }
        protected override void OnDispose()
        {
            m_Camera = null;
            m_Movement = null;
        }
        protected override void OnExecute(InputAction.CallbackContext target)
        {
            m_Movement.MoveOffset = MoveOffset;
            m_Movement.MoveSpeed = MoveSpeed;

            m_Movement.AxisVelocity = target.ReadValue<Vector2>();
        }
    }
}
