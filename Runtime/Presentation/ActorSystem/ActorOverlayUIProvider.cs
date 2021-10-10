using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// <see cref="ActorOverlayUIAttributeBase"/> 와 페어로 작동합니다.
    /// </summary>
    public class ActorOverlayUIProvider : ActorProviderBase
    {
        [JsonProperty(Order = -10, PropertyName = "UIEntries")]
        protected Reference<ActorOverlayUIEntry>[] m_UIEntries = Array.Empty<Reference<ActorOverlayUIEntry>>();

        //[JsonIgnore] private Entity<UIObjectEntity> m_InstanceObject = Entity<UIObjectEntity>.Empty;

        //[JsonIgnore] private UpdateJob m_UpdateJob;
        //[JsonIgnore] private CoroutineJob m_UpdateCoroutine;

        //[JsonIgnore] protected override Type[] ReceiveEventOnly => new Type[]
        //    {
        //        TypeHelper.TypeOf<IActorOverlayUIEvent>.Type
        //    };
        //[JsonIgnore] public Entity<UIObjectEntity> InstanceObject => m_InstanceObject;

        protected override sealed void OnCreated(Entity<ActorEntity> entity, WorldCanvasSystem worldCanvasSystem)
        {
            for (int i = 0; i < m_UIEntries.Length; i++)
            {
                ActorOverlayUIEntry entry = m_UIEntries[i].GetObject();
                if (!entry.m_CreateOnStart) continue;

                worldCanvasSystem.RegisterActorOverlayUI(entity, m_UIEntries[i]);
            }
        }
        protected override sealed void OnDestroy(Entity<ActorEntity> entity, WorldCanvasSystem worldCanvasSystem)
        {
            worldCanvasSystem.RemoveAllOverlayUI(entity.Cast<ActorEntity, IEntity>());
            //if (m_InstanceObject.IsValid())
            //{
            //    m_InstanceObject.Destroy();
            //}

            //if (m_UpdateCoroutine.IsValid())
            //{
            //    m_UpdateCoroutine.Stop();
            //    m_UpdateJob = default;
            //}
        }
        protected override sealed void OnEventReceived<TEvent>(TEvent ev, WorldCanvasSystem worldCanvasSystem)
        {
            ActorEventHandler(ev);
            worldCanvasSystem.PostActorOverlayUIEvent(Parent.As<IEntityData, ActorEntity>(), ev);

            //if (ev is IActorOverlayUIEvent overlayEv)
            //{
            //    //overlayEv.OnExecute(m_InstanceObject);
            //    ActorOverlayUIEventHandler(ev);
            //    //if (InstanceObject.IsValid())
            //    //{
            //    //    InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().UIEventReceived(ev);
            //    //}
            //}
            //else
            //{
            //    //ActorEventHandler(ev);
            //    //if (InstanceObject.IsValid())
            //    //{
            //    //    InstanceObject.GetAttribute<ActorOverlayUIAttributeBase>().EventReceived(ev);
            //    //}
            //}
        }

//        /// <summary>
//        /// <see cref="IActorOverlayUIEvent"/> 만 들어옵니다.
//        /// </summary>
//        /// <typeparam name="TEvent"></typeparam>
//        /// <param name="ev"></param>
//        protected virtual void ActorOverlayUIEventHandler<TEvent>(TEvent ev)
//#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
//            where TEvent : struct, IActorEvent
//#else
//            where TEvent : unmanaged, IActorEvent
//#endif
//        { }
        protected virtual void ActorEventHandler<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        { }
    }
}
