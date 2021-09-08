using Cinemachine;
using Syadeu.Database;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    public sealed class TRPGCameraMovement : AdditionalCameraComponent
    {
        private CinemachineTargetGroup m_TargetGroup;

        private Transform m_DefaultTarget = null;
        private ITransform m_TargetTransform = null;
        private float3 m_TargetPosition = 0;
        private quaternion m_TargetOrientation = quaternion.EulerZXY(new float3(45, 45, 0));

        public float2 AxisVelocity
        {
            get
            {
                float3
                    forward = Vector3.ProjectOnPlane(RenderSystem.Camera.transform.forward, Vector3.up),
                    currentPos = m_TargetGroup.transform.position,
                    project = Vector3.ProjectOnPlane(math.normalize(currentPos - TargetPosition), Vector3.up);

                quaternion rot = quaternion.LookRotation(forward, math.up());

                float
                    horizontal = math.dot(project, math.mul(rot, math.right())),
                    vertical = math.dot(project, math.mul(rot, math.forward()));

                return new float2(horizontal, vertical);
            }
            set
            {
                if (RenderSystem == null)
                {
                    "null return".ToLog();
                    return;
                }

                float3 
                    forward = Vector3.ProjectOnPlane(RenderSystem.Camera.transform.forward, Vector3.up),
                    velocity = math.normalize(new float3(value.x, 0, value.y));

                quaternion rot = quaternion.LookRotation(forward, math.up());
                float4x4 vp = new float4x4(rot, float3.zero);
                float3
                    point = math.normalize(math.mul(vp, new float4(velocity, 1)).xyz),
                    targetVelocity = point * MoveOffset;

                TargetPosition += targetVelocity;
            }
        }
        public float MoveOffset { get; set; } = .07f;
        public float MoveSpeed { get; set; } = 8;
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
        public quaternion TargetOrientation
        {
            get
            {
                return m_TargetOrientation;
            }
            set
            {
                m_TargetOrientation = value;
            }
        }

        protected override void OnInitialize(Camera camera, CinemachineBrain brain, CinemachineTargetGroup targetGroup)
        {
            m_TargetGroup = targetGroup;
            m_TargetPosition = m_TargetGroup.transform.position;

            GameObject target = new GameObject("Default Target");
            m_DefaultTarget = target.transform;
            m_DefaultTarget.SetParent(transform.parent);
            m_DefaultTarget.position = m_TargetPosition;

            m_TargetGroup.AddMember(m_DefaultTarget, 1, 1);
        }

        protected override void OnRenderStart()
        {
            StartCoroutine(Updater());
        }

        private IEnumerator Updater()
        {
            WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
            Transform groupTr = m_TargetGroup.transform;
            
            while (m_TargetGroup != null)
            {
                m_DefaultTarget.position = TargetPosition;
                CameraComponent.CurrentCamera.VirtualCameraGameObject.transform.rotation
                    = TargetOrientation;

                //groupTr.position = math.lerp(groupTr.position, TargetPosition, Time.deltaTime * MoveSpeed);

                //quaternion originRot = CameraComponent.Brain.ActiveVirtualCamera.VirtualCameraGameObject.transform.rotation;
                //CameraComponent.Brain.ActiveVirtualCamera.VirtualCameraGameObject.transform.rotation = new quaternion(math.lerp(originRot.value, TargetOrientation.value, Time.deltaTime * MoveSpeed));

                yield return waitForFixedUpdate;
            }
        }

        public void SetTarget(Transform tr)
        {
            m_TargetTransform = new CustomTransform(tr);
        }
        public void SetTarget(ITransform tr)
        {
            m_TargetTransform = tr;
        }
    }
}
