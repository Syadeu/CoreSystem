using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;

namespace Syadeu.Presentation.Actor
{
    public struct ActorControllerComponent : IEntityComponent, IDisposable
    {
        internal PresentationSystemID<EntitySystem> m_EntitySystem;

        internal Entity<ActorEntity> m_Parent;
        internal InstanceArray<ActorProviderBase> m_InstanceProviders;
        internal ReferenceArray<Reference<ParamAction<IActorEvent>>> m_OnEventReceived;

        public void ScheduleEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorSystem system = PresentationSystem<ActorSystem>.System;
            system.ScheduleEvent(PostEvent, ev);
        }
        public void PostEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            try
            {
                ev.OnExecute(m_Parent);
            }
            catch (Exception ex)
            {
                CoreSystem.Logger.LogError(Channel.Entity, ex);
                return;
            }

            if (ev is ActorLifetimeChangedEvent lifeTimeChanged)
            {
                ActorStateAttribute state = m_Parent.GetAttribute<ActorStateAttribute>();
                if (state != null)
                {
                    if (lifeTimeChanged.LifeTime == ActorLifetimeChangedEvent.State.Alive)
                    {
                        if ((state.State & ActorStateAttribute.StateInfo.Spawn) != ActorStateAttribute.StateInfo.Spawn)
                        {
                            state.State = ActorStateAttribute.StateInfo.Spawn;
                        }
                    }
                    else
                    {
                        if ((state.State & ActorStateAttribute.StateInfo.Dead) != ActorStateAttribute.StateInfo.Dead)
                        {
                            state.State = ActorStateAttribute.StateInfo.Dead;
                        }
                    }
                }
            }

            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                ExecutePostEvent(m_InstanceProviders[i].Object, ev);
            }
            m_OnEventReceived.Execute(ev);

            if (ev is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        private void ExecutePostEvent<TEvent>(IActorProvider provider, TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            //Type evType = TypeHelper.TypeOf<TEvent>.Type;
            //bool executable;
            //if (m_ProviderAcceptsOnly.TryGetValue(provider, out var types))
            //{
            //    executable = false;
            //    for (int i = 0; i < types.Length; i++)
            //    {
            //        if (types[i].IsAssignableFrom(evType))
            //        {
            //            executable = true;
            //            break;
            //        }
            //    }
            //}
            //else executable = true;

            //if (!executable) return;

            provider.ReceivedEvent(ev);
        }

        public Instance<T> GetProvider<T>() where T : ActorProviderBase
        {
            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                if (m_InstanceProviders[i].Object is T) return m_InstanceProviders[i].Cast<ActorProviderBase, T>();
            }
            return Instance<T>.Empty;
        }

        public void Dispose()
        {
            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                ExecuteOnDestroy(m_InstanceProviders[i].Object, m_Parent);
                m_EntitySystem.System.DestroyObject(m_InstanceProviders[i]);
            }

            m_InstanceProviders.Dispose();
            m_OnEventReceived.Dispose();
            "dispose in".ToLog();
        }
        private static void ExecuteOnDestroy(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnDestroy(entity);
        }
    }
}
