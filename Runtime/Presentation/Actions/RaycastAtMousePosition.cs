using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.Actions
{
    /// <summary>
    /// 마우스 좌표에 <see cref="EntityRaycastSystem"/> 으로 레이캐스팅합니다.
    /// </summary>
    [DisplayName("InstanceAction: Raycast At Mouse Position")]
    [ReflectionDescription(
        "마우스 좌표에 EntityRaycastSystem 으로 레이캐스팅합니다.")]
    public sealed class RaycastAtMousePosition : InstanceAction
    {
        [Header("TriggerActions")]
        [JsonProperty(Order = 0, PropertyName = "OnHit")]
        private Reference<TriggerAction>[] m_OnHit = Array.Empty<Reference<TriggerAction>>();

        [Header("Actions")]
        private Reference<InstanceAction>[] m_OnHitAction = Array.Empty<Reference<InstanceAction>>();

        [JsonIgnore] private RenderSystem m_RenderSystem;
        [JsonIgnore] private EntityRaycastSystem m_RaycastSystem;

        [JsonIgnore] private RaycastInfo m_RaycastInfo;

        protected override void OnCreated()
        {
            m_RenderSystem = PresentationSystem<RenderSystem>.System;
            m_RaycastSystem = PresentationSystem<EntityRaycastSystem>.System;

            CoreSystem.Logger.NotNull(m_RenderSystem);
            CoreSystem.Logger.NotNull(m_RaycastSystem);
        }

        protected override void OnExecute()
        {
            Ray ray = m_RenderSystem.ScreenPointToRay(new float3(Mouse.current.position.ReadValue(), 0));
            if (!m_RaycastSystem.Raycast(in ray, out m_RaycastInfo)) return;

            m_OnHit.Execute(m_RaycastInfo.entity.As<IEntity, IEntityData>());
            m_OnHitAction.Execute();
        }
    }
}
