using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Syadeu.Presentation.Actions
{
    /// <summary>
    /// 마우스 좌표에 <see cref="EntityRaycastSystem"/> 으로 레이캐스팅합니다.
    /// </summary>
    [DisplayName("InstanceAction: Raycast All At Mouse Position")]
    [ReflectionDescription(
        "마우스 좌표에 EntityRaycastSystem 으로 레이캐스팅합니다.")]
    public sealed class RaycastAllAtMousePosition : InstanceAction
    {
        [Header("TriggerActions")]
        [JsonProperty(Order = 0, PropertyName = "OnHit")]
        private Reference<TriggerAction>[] m_OnHit = Array.Empty<Reference<TriggerAction>>();

        [JsonIgnore] private RenderSystem m_RenderSystem;
        [JsonIgnore] private EntityRaycastSystem m_RaycastSystem;

        [JsonIgnore] private List<RaycastInfo> m_RaycastInfos;

        protected override void OnCreated()
        {
            m_RenderSystem = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System;
            m_RaycastSystem = PresentationSystem<DefaultPresentationGroup, EntityRaycastSystem>.System;

            m_RaycastInfos = new List<RaycastInfo>();

            CoreSystem.Logger.NotNull(m_RenderSystem);
            CoreSystem.Logger.NotNull(m_RaycastSystem);
        }

        protected override void OnReserve()
        {
            m_RaycastInfos.Clear();
        }
        protected override void OnDestroy()
        {
            m_RenderSystem = null;
            m_RaycastSystem = null;
        }

        protected override void OnExecute()
        {
            Ray ray = m_RenderSystem.ScreenPointToRay(new float3(Mouse.current.position.ReadValue(), 0));
            m_RaycastSystem.RaycastAll(m_RaycastInfos, in ray);

            for (int i = 0; i < m_RaycastInfos.Count; i++)
            {
                m_OnHit.Execute(m_RaycastInfos[i].entity.As<IEntity, IEntityData>());
            }
        }
    }
}
