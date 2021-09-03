using Cinemachine;
using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Render;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGCameraMovement : AdditionalCameraComponent
    {
        private CinemachineTargetGroup m_TargetGroup;

        private bool m_WasZero = false;
        private Transform m_TargetTransform = null;
        private float3 m_TargetPosition = 0;

        public float2 AxisVelocity
        {
            get
            {
                float3 currentPos = m_TargetGroup.transform.position;
                return math.normalize(currentPos - TargetPosition).xz;
            }
            set
            {
                if (RenderSystem == null)
                {
                    "null return".ToLog();
                    return;
                }
                if (value.Equals(float2.zero))
                {
                    if (m_WasZero) return;

                    m_WasZero = true;
                }
                else m_WasZero = false;

                float3 
                    forward = Vector3.ProjectOnPlane(RenderSystem.Camera.transform.forward, Vector3.up),
                    velocity = math.normalize(new float3(value.x, 0, value.y));

                quaternion rot = quaternion.LookRotation(forward, new float3(0, 1, 0));
                float4x4 vp = new float4x4(rot, float3.zero);
                float3 point = math.mul(vp, new float4(velocity, 1)).xyz;

                TargetPosition += point * MoveOffset;
            }
        }
        public float MoveOffset { get; set; } = .07f;
        public float MoveSpeed { get; set; } = 4;
        public float3 TargetPosition
        {
            get
            {
                if (m_TargetTransform == null) return m_TargetPosition;
                return m_TargetTransform.position;
            }
            set
            {
                m_TargetTransform = null;

                m_TargetPosition = value;
            }
        }

        protected override void OnInitialize(Camera camera, CinemachineBrain brain, CinemachineTargetGroup targetGroup)
        {
            m_TargetGroup = targetGroup;
            m_TargetPosition = m_TargetGroup.transform.position;


            StartCoroutine(Updater());
        }

        protected override void OnRenderStart()
        {
            StartCoroutine(Updater());
        }

        private IEnumerator Updater()
        {
            Transform groupTr = m_TargetGroup.transform;
            while (m_TargetGroup != null)
            {
                groupTr.position = math.lerp(groupTr.position, TargetPosition, Time.deltaTime * MoveSpeed);

                yield return null;
            }
        }
    }
    public sealed class TRPGCameraMovementParamAction : ParamAction<InputAction.CallbackContext>
    {
        [JsonIgnore] private CameraComponent m_Camera;
        [JsonIgnore] private TRPGCameraMovement m_Movement;

        protected override void OnCreated()
        {
            CoreSystem.Logger.True(CameraComponent.HasInstance, "Camera null");

            m_Camera = CameraComponent.Instance;
            m_Movement = m_Camera.GetCameraComponent<TRPGCameraMovement>();

            CoreSystem.Logger.NotNull(m_Camera);
            CoreSystem.Logger.NotNull(m_Movement);
        }
        protected override void OnExecute(InputAction.CallbackContext target)
        {
            m_Movement.AxisVelocity = target.ReadValue<Vector2>();
        }
    }
}
