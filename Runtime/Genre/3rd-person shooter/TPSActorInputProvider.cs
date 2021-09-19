using Newtonsoft.Json;
using Syadeu.Presentation;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Input;
using Syadeu.Presentation.Render;
using System.Collections;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

public partial class SimplePlayerController
{
    [DisplayName("ActorProvider: TPS Input Provider")]
    public sealed class TPSActorInputProvider : ActorProviderBase
    {
        public enum MoveType
        {
            Walk,
            Run
        }

        [Header("Animator")]
        [JsonProperty]
        private float m_AnimatorSpeed = 4;
        [JsonProperty]
        private string m_HorizontalKeyString = "Horizontal";
        [JsonProperty]
        private string m_VerticalKeyString = "Vertical";
        [JsonProperty]
        private string m_SpeedKeyString = "Speed";

        [JsonIgnore] private InputSystem m_InputSystem = null;
        [JsonIgnore] private RenderSystem m_RenderSystem = null;
        [JsonIgnore] private AnimatorAttribute m_Animator = null;

        [JsonIgnore] private UpdateJob m_Update;
        [JsonIgnore] private CoroutineJob m_UpdateJob = CoroutineJob.Null;

        [JsonIgnore] public float2 Axis { get; set; }
        [JsonIgnore] public MoveType Move { get; set; } = MoveType.Run;

        protected override void OnCreated(Entity<ActorEntity> entity)
        {
            m_InputSystem = PresentationSystem<InputSystem>.System;
            m_RenderSystem = PresentationSystem<RenderSystem>.System;
            

            m_Update = new UpdateJob(entity, this);
            m_UpdateJob = StartCoroutine(m_Update);
        }
        protected override void OnDestroy(Entity<ActorEntity> entity)
        {
            m_UpdateJob.Stop();

            m_InputSystem = null;
            m_RenderSystem = null;
            m_Animator = null;
        }

        private struct UpdateJob : ICoroutineJob
        {
            private Entity<ActorEntity> m_Entity;
            private Instance<TPSActorInputProvider> m_InputProvider;
            private int 
                m_HorizontalKey, m_VerticalKey, m_SpeedKey;
            private float m_AnimatorSpeed;

            public UpdateLoop Loop => UpdateLoop.Transform;

            public UpdateJob(Entity<ActorEntity> entity, TPSActorInputProvider inputProvider)
            {
                m_Entity = entity;
                m_InputProvider = new Instance<TPSActorInputProvider>(inputProvider);
                m_HorizontalKey = Animator.StringToHash(inputProvider.m_HorizontalKeyString);
                m_VerticalKey = Animator.StringToHash(inputProvider.m_VerticalKeyString);
                m_SpeedKey = Animator.StringToHash(inputProvider.m_SpeedKeyString);
                m_AnimatorSpeed = inputProvider.m_AnimatorSpeed;
            }
            public void Dispose()
            {
            }
            public IEnumerator Execute()
            {
                var inputSystem = PresentationSystem<InputSystem>.System;
                var renderSystem = PresentationSystem<RenderSystem>.System;
                var animator = m_Entity.GetAttribute<AnimatorAttribute>();
                if (animator == null) yield break;

                var input = m_InputProvider.Object;
                var tr = m_Entity.transform;

                while (!animator.IsInitialized)
                {
                    yield return null;
                }

                while (m_Entity.IsValid())
                {
                    if (renderSystem.Camera == null || 
                        !inputSystem.EnableInput)
                    {
                        yield return null;
                        continue;
                    }

                    // test
                    input.Axis = new float2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                    //

                    float3 camFor = math.mul(renderSystem.LastCameraData.orientation, math.forward());
                    Vector3 camForward = Vector3.ProjectOnPlane(camFor, Vector3.up);

                    float
                        currentSpeed = animator.GetFloat(m_SpeedKey),
                        currentX = animator.GetFloat(m_HorizontalKey),
                        currentZ = animator.GetFloat(m_VerticalKey);

                    Vector3
                        movement = new Vector3(input.Axis.x, 0, input.Axis.y),
                        norm = movement.normalized;

                    quaternion rot = quaternion.LookRotation(camForward, math.up());
                    float4x4 vp = new float4x4(new float3x3(rot), float3.zero);
                    float3 point = math.mul(vp, new float4(norm, 1)).xyz;

                    float
                        horizontal = Vector3.Dot(point, tr.right),
                        vertical = Vector3.Dot(point, tr.forward);

                    float speed;
                    if (vertical > .1f)
                    {
                        speed = math.lerp(currentSpeed, input.Move == MoveType.Walk ? 1 : 2, m_AnimatorSpeed * Time.deltaTime);
                    }
                    else if (vertical < -.1f)
                    {
                        speed = math.lerp(currentSpeed, input.Move == MoveType.Walk ? -1 : -2, m_AnimatorSpeed * Time.deltaTime);
                    }
                    else speed = math.lerp(currentSpeed, 0, m_AnimatorSpeed * Time.deltaTime);
                    float
                    //    speed = Mathf.Lerp(currentSpeed, vertical < 0 ? -norm.magnitude : norm.magnitude, m_AnimatorSpeed * Time.deltaTime),

                        x = Mathf.Lerp(currentX, horizontal, m_AnimatorSpeed * Time.deltaTime),
                        z = Mathf.Lerp(currentZ, vertical, m_AnimatorSpeed * Time.deltaTime);

                    animator.SetFloat(m_SpeedKey, speed);
                    animator.SetFloat(m_HorizontalKey, x);
                    animator.SetFloat(m_VerticalKey, z);

                    yield return null;
                }
            }
        }
    }
}
