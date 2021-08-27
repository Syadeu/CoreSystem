using Newtonsoft.Json;
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
    [DisplayName("Action: Raycast At Mouse Position")]
    public sealed class RaycastAtMousePositionAction : InstanceAction
    {
        [Header("TriggerActions")]
        [JsonProperty(Order = 0, PropertyName = "OnHit")]
        private Reference<TriggerActionBase>[] m_OnHit = Array.Empty<Reference<TriggerActionBase>>();

        [JsonIgnore] private RenderSystem m_RenderSystem;
        [JsonIgnore] private EntityRaycastSystem m_RaycastSystem;

        [JsonIgnore] private List<RaycastInfo> m_RaycastInfos;

        protected override void OnCreated()
        {
            m_RenderSystem = PresentationSystem<RenderSystem>.System;
            m_RaycastSystem = PresentationSystem<EntityRaycastSystem>.System;

            m_RaycastInfos = new List<RaycastInfo>();

            CoreSystem.Logger.NotNull(m_RenderSystem);
            CoreSystem.Logger.NotNull(m_RaycastSystem);
        }

        protected override void OnTerminate()
        {
            m_RaycastInfos.Clear();
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
