using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorStatAttribute : ActorAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Stats")] private ValuePairContainer m_Stats = new ValuePairContainer();

        [JsonIgnore] private ValuePairContainer m_CurrentStats;
        [JsonIgnore] private ActorEventHandler m_EventHandler;

        public event Action<Hash, object> OnValueChanged;

        internal void Initialize()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();
            m_EventHandler = new ActorEventHandler();
        }
        protected override void OnDispose()
        {
            m_CurrentStats = null;

            base.OnDispose();
        }

        public void Reset()
        {
            m_CurrentStats = (ValuePairContainer)m_Stats.Clone();
        }
        public T GetOriginalValue<T>(string name) => m_Stats.GetValue<T>(name);
        public T GetOriginalValue<T>(Hash hash) => m_Stats.GetValue<T>(hash);
        public T GetValue<T>(string name) => m_CurrentStats.GetValue<T>(name);
        public T GetValue<T>(Hash hash) => m_CurrentStats.GetValue<T>(hash);
        public void SetValue<T>(string name, T value) => SetValue(ToValueHash(name), value);
        public void SetValue<T>(Hash hash, T value)
        {
            m_CurrentStats.SetValue(hash, value);
            OnValueChanged?.Invoke(hash, value);

            ActorControllerAttribute ctr = Parent.GetAttribute<ActorControllerAttribute>();
            Entity<ActorEntity> entity = Parent.CastAs<IEntityData, ActorEntity>();
            for (int i = 0; i < m_EventHandler.Length; i++)
            {
                IActorStatEvent ev = (IActorStatEvent)m_EventHandler[i];
                if (!ev.TargetValueNameHash.Equals(hash)) continue;

                m_EventHandler.Invoke(i, entity);
                //if (ctr != null)
                //{
                //    ctr.PostEvent(ev);
                //}
            }
        }

        public void RegisterEvent<T>(T ev) where T : unmanaged, IActorStatEvent
        {
            m_EventHandler.Add(ev);
        }
        public void UnregisterEvent<T>(T ev) where T : unmanaged, IActorStatEvent
        {
            m_EventHandler.Remove(ev);
        }
        public void UnregisterEvent(ActorEventID id)
        {
            m_EventHandler.Remove(id);
        }

        public void FireEvent(ActorEventID id)
        {
            if (!m_EventHandler.Invoke(id, Parent.CastAs<IEntityData, ActorEntity>()))
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Firing event({id}) has faild.");
                return;
            }
        }

        public static Hash ToValueHash(string name) => Hash.NewHash(name);
    }
    [Preserve]
    internal sealed class ActorStatProcessor : AttributeProcessor<ActorStatAttribute>
    {
        protected override void OnCreated(ActorStatAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.Initialize();
        }
    }

    public interface IActorStatEvent : IActorEvent
    {
        Hash TargetValueNameHash { get; }
    }
}
