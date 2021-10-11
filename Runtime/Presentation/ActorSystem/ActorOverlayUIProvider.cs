using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorOverlayUIAttributeBase"/> 와 페어로 작동합니다.
    /// </summary>
    public class ActorOverlayUIProvider : ActorProviderBase,
        INotifyComponent<ActorOverlayUIComponent>
    {
        [JsonProperty(Order = -10, PropertyName = "UIEntries")]
        protected Reference<ActorOverlayUIEntry>[] m_UIEntries = Array.Empty<Reference<ActorOverlayUIEntry>>();

        [JsonIgnore] public IReadOnlyList<Reference<ActorOverlayUIEntry>> UIEntries => m_UIEntries;

        protected override sealed void OnCreated(Entity<ActorEntity> entity, WorldCanvasSystem worldCanvasSystem)
        {
            ActorOverlayUIComponent uiComponent = new ActorOverlayUIComponent()
            {
                m_OpenedUI = new FixedList512Bytes<Reference<ActorOverlayUIEntry>>()
            };
            entity.AddComponent(uiComponent);

            for (int i = 0; i < m_UIEntries.Length; i++)
            {
                ActorOverlayUIEntry entry = m_UIEntries[i].GetObject();
                if (!entry.m_EnableAlways) continue;

                worldCanvasSystem.RegisterActorOverlayUI(entity, m_UIEntries[i]);
            }
        }
        protected override sealed void OnDestroy(Entity<ActorEntity> entity, WorldCanvasSystem worldCanvasSystem)
        {
            worldCanvasSystem.RemoveAllOverlayUI(entity.Cast<ActorEntity, IEntity>());
        }
        protected override sealed void OnEventReceived<TEvent>(TEvent ev, WorldCanvasSystem worldCanvasSystem)
        {
            ActorEventHandler(ev);
            worldCanvasSystem.PostActorOverlayUIEvent(Parent.As<IEntityData, ActorEntity>(), ev);
        }

        protected virtual void ActorEventHandler<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        { }
    }
    public struct ActorOverlayUIComponent : IEntityComponent
    {
        internal FixedList512Bytes<Reference<ActorOverlayUIEntry>> m_OpenedUI;
    }
}
