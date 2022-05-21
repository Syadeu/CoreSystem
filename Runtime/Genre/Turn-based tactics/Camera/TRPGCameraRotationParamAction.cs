﻿using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Render;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGCameraRotationParamAction : ParamAction<InputAction.CallbackContext>
    {
        [JsonProperty] public float MoveOrientation { get; set; } = 45;

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
        protected override void OnReserve()
        {
            m_Camera = null;
            m_Movement = null;
        }
        protected override void OnExecute(InputAction.CallbackContext target)
        {
            float3 eulerAngles = m_Movement.TargetOrientation;
            $"in => {eulerAngles.y} + {MoveOrientation}".ToLog();
            eulerAngles.y += MoveOrientation;
            $"in2 => {eulerAngles.y}".ToLog();
            //if (eulerAngles.x >= 360) eulerAngles.x -= 360;
            //else if (eulerAngles.x <= -360) eulerAngles.x += 360;

            m_Movement.TargetOrientation = eulerAngles;
        }
    }
}
