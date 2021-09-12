using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorControllerAttribute : ActorAttributeBase
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "SetAliveOnCreated")]
        internal bool m_SetAliveOnCreated = true;
        [JsonProperty(Order = 1, PropertyName = "OnReceivedEvent")]
        private Reference<ParamAction<IActorEvent>>[] m_OnReceivedEvent = Array.Empty<Reference<ParamAction<IActorEvent>>>();

        [Header("Provider")]
        [JsonProperty(Order = 2, PropertyName = "Providers")] 
        internal Reference<ActorProviderBase>[] m_Providers = Array.Empty<Reference<ActorProviderBase>>();

        [JsonIgnore] internal ActorProviderBase[] Providers { get; set; }

        public void PostEvent<TEvent>(TEvent ev) where TEvent : unmanaged, IActorEvent
        {
            ev.OnExecute(Parent.CastAs<IEntityData, ActorEntity>());

            for (int i = 0; i < Providers.Length; i++)
            {
                ExecutePostEvent(Providers[i], ev);
            }
            m_OnReceivedEvent.Execute(ev);
        }
        private void ExecutePostEvent<TEvent>(IActorProvider provider, TEvent ev) where TEvent : unmanaged, IActorEvent
        {
            provider.ReceivedEvent(ev);
        }

        public T GetProvider<T>() where T : ActorProviderBase
        {
            for (int i = 0; i < Providers.Length; i++)
            {
                if (Providers[i] is T) return (T)Providers[i];
            }
            return null;
        }
    }
    internal sealed class ActorControllerProcessor : AttributeProcessor<ActorControllerAttribute>
    {
        protected override void OnCreated(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            Entity<ActorEntity> actor = entity.CastAs<IEntityData, ActorEntity>();

            attribute.Providers = new ActorProviderBase[attribute.m_Providers.Length];
            for (int i = 0; i < attribute.m_Providers.Length; i++)
            {
                ActorProviderBase clone = (ActorProviderBase)attribute.m_Providers[i].GetObject().Clone();
                Initialize(actor, clone);
                attribute.Providers[i] = clone;
            }

            for (int i = 0; i < attribute.Providers.Length; i++)
            {
                ExecuteOnCreated(attribute.Providers[i], actor);
            }

            if (attribute.m_SetAliveOnCreated)
            {
                ActorLifetimeChangedEvent ev = new ActorLifetimeChangedEvent(ActorLifetimeChangedEvent.State.Alive);
                attribute.PostEvent(ev);
            }
        }
        protected override void OnDestroy(ActorControllerAttribute attribute, EntityData<IEntityData> entity)
        {
            Entity<ActorEntity> actor = entity.CastAs<IEntityData, ActorEntity>();
            for (int i = 0; i < attribute.Providers.Length; i++)
            {
                ExecuteOnDestroy(attribute.Providers[i], actor);
                attribute.Providers[i].Dispose();
            }

            attribute.Providers = null;
        }
        private void Initialize(Entity<ActorEntity> parent, IActorProvider provider)
        {
            provider.Bind(parent, EventSystem, EntitySystem, EntitySystem.m_CoroutineSystem);
        }
        private void ExecuteOnCreated(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnCreated(entity);
        }
        private void ExecuteOnDestroy(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnDestroy(entity);
        }
    }
}
