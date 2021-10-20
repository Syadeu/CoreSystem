#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using Unity.Burst;
using Unity.Jobs;

namespace Syadeu.Presentation.Actor
{
    public struct ActorControllerComponent : IEntityComponent, IDisposable
    {
        internal PresentationSystemID<EntitySystem> m_EntitySystem;

        internal Entity<ActorEntity> m_Parent;
        internal FixedInstanceList64<ActorProviderBase> m_InstanceProviders;
        internal FixedReferenceList64<ParamAction<IActorEvent>> m_OnEventReceived;

        public bool IsBusy()
        {
            ActorSystem system = PresentationSystem<DefaultPresentationGroup, ActorSystem>.System;
            if (!system.CurrentEventActor.IsEmpty())
            {
                if (system.CurrentEventActor.Idx.Equals(m_Parent.Idx))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary><inheritdoc cref="ScheduleEvent{TEvent}(TEvent)"/></summary>
        /// <remarks>
        /// <paramref name="overrideSameEvent"/> 가 true 일 경우에 다음 이벤트가 없을 경우에만 
        /// 같은 이벤트를 덮어씁니다.
        /// </remarks>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        /// <param name="overrideSameEvent"></param>
        public void ScheduleEvent<TEvent>(TEvent ev, bool overrideSameEvent)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorSystem system = PresentationSystem<DefaultPresentationGroup, ActorSystem>.System;
            system.ScheduleEvent(m_Parent, PostEvent, ev, overrideSameEvent);
        }
        /// <summary>
        /// 이벤트를 스케쥴합니다.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="ev"></param>
        public void ScheduleEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            ActorSystem system = PresentationSystem<DefaultPresentationGroup, ActorSystem>.System;
            system.ScheduleEvent(m_Parent, PostEvent, ev);
        }

        [BurstCompile]
        private struct EventJob<TEvent> : IJob
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            public Entity<ActorEntity> m_Entity;
            public TEvent m_Event;

            public void Execute()
            {
                m_Event.OnExecute(m_Entity);
            }
        }
        private void InternalPostEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            if (ev.BurstCompile)
            {
                EventJob<TEvent> eventJob = new EventJob<TEvent>()
                {
                    m_Entity = m_Parent,
                    m_Event = ev
                };
                eventJob.Run();
            }
            else
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
            }
        }

        public void PostEvent<TEvent>(TEvent ev)
#if UNITY_EDITOR && ENABLE_UNITY_COLLECTIONS_CHECKS
            where TEvent : struct, IActorEvent
#else
            where TEvent : unmanaged, IActorEvent
#endif
        {
            InternalPostEvent(ev);

            if (ev is ActorLifetimeChangedEvent lifeTimeChanged)
            {
                ActorStateAttribute state = m_Parent.GetAttribute<ActorStateAttribute>();
                if (state != null)
                {
                    if (lifeTimeChanged.LifeTime == ActorLifetimeChangedEvent.State.Alive)
                    {
                        if ((state.State & ActorStateAttribute.StateInfo.Spawn | ActorStateAttribute.StateInfo.Idle) != (ActorStateAttribute.StateInfo.Spawn | ActorStateAttribute.StateInfo.Idle))
                        {
                            state.State = ActorStateAttribute.StateInfo.Spawn | ActorStateAttribute.StateInfo.Idle;
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
                ExecutePostEvent(m_InstanceProviders[i].GetObject(), ev);
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

        public bool HasProvider<T>() where T : ActorProviderBase
        {
            if (TypeHelper.TypeOf<T>.IsAbstract)
            {
                for (int i = 0; i < m_InstanceProviders.Length; i++)
                {
                    if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(m_InstanceProviders[i].GetObject().GetType()))
                    {
                        return true;
                    }
                }

                return false;
            }

            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                if (m_InstanceProviders[i].GetObject() is T)
                {
                    return true;
                }
            }
            return false;
        }
        public Instance<T> GetProvider<T>() where T : ActorProviderBase
        {
            if (TypeHelper.TypeOf<T>.IsAbstract)
            {
                for (int i = 0; i < m_InstanceProviders.Length; i++)
                {
                    if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(m_InstanceProviders[i].GetObject().GetType()))
                    {
                        return m_InstanceProviders[i].Cast<ActorProviderBase, T>();
                    }
                }

                return Instance<T>.Empty;
            }

            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                if (m_InstanceProviders[i].GetObject() is T)
                {
                    return m_InstanceProviders[i].Cast<ActorProviderBase, T>();
                }
            }
            return Instance<T>.Empty;
        }

        void IDisposable.Dispose()
        {
            for (int i = 0; i < m_InstanceProviders.Length; i++)
            {
                ExecuteOnDestroy(m_InstanceProviders[i].GetObject(), m_Parent);
                m_EntitySystem.System.DestroyObject(m_InstanceProviders[i]);
            }
        }
        private static void ExecuteOnDestroy(IActorProvider provider, Entity<ActorEntity> entity)
        {
            provider.OnDestroy(entity);
        }
    }
}
