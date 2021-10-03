using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using UnityEngine;
using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Proxy;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: TRPG ActionPoint UI")]
    [Description("UIObjectEntity 에 달리는 어트리뷰트")]
    public sealed class TRPGActionPointUIAttribute : ActorOverlayUIAttributeBase
    {
        [Header("General")]
        [JsonProperty(Order = 0, PropertyName = "HPStatName")]
        internal string m_HPStatName = "HP";

        [JsonIgnore] internal Hash m_HPNameHash;
        [JsonIgnore] internal TRPGActionPointOverlayUI m_CurrentProxy = null;

        protected override void OnUICreated(Entity<ActorEntity> parent)
        {
            m_HPNameHash = ActorStatAttribute.ToValueHash(m_HPStatName);
        }
        protected override void OnUIEventReceived<TEvent>(TEvent ev)
        {
            if (ev is TRPGActorActionPointChangedUIEvent apChangedEv)
            {
                if (m_CurrentProxy != null)
                {
                    m_CurrentProxy.SetAPText(apChangedEv.To);
                }
            }
        }
        protected override void OnEventReceived<TEvent>(TEvent ev)
        {
            if (ev is ActorHitEvent hitEvent)
            {
                ActorHitEventHandler(hitEvent);
            }
        }

        private void ActorHitEventHandler(ActorHitEvent ev)
        {
            if (m_CurrentProxy == null) return;

            ActorStatAttribute stat = ParentEntity.GetAttribute<ActorStatAttribute>();
            if (stat == null)
            {
                "no stat".ToLogError();
                return;
            }

            int hp = stat.GetValue<int>(m_HPNameHash);

            m_CurrentProxy.SetHPText(hp);
        }
    }
    internal sealed class TRPGActionPointUIProcessor : AttributeProcessor<TRPGActionPointUIAttribute>,
        IAttributeOnProxy
    {
        public void OnProxyCreated(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            TRPGActionPointUIAttribute att = (TRPGActionPointUIAttribute)attribute;

            if (monoObj is TRPGActionPointOverlayUI ui)
            {
                att.m_CurrentProxy = ui;
            }
#if UNITY_EDITOR
            else
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity ({entity.RawName}) prefab doesn\'t have {nameof(TRPGActionPointOverlayUI)} Monobehaviour.");
            }
#endif

            Setup(att.ParentEntity, in att);
        }
        public void OnProxyRemoved(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            TRPGActionPointUIAttribute att = (TRPGActionPointUIAttribute)attribute;

            att.m_CurrentProxy = null;
        }

        private static void Setup(in Entity<ActorEntity> entity, in TRPGActionPointUIAttribute att)
        {
            if (!entity.HasComponent<TurnPlayerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.RawName}) is not an TurnPlayer ?");
                return;
            }

            var turn = entity.GetComponent<TurnPlayerComponent>();
            att.m_CurrentProxy.SetAPFullText(turn.MaxActionPoint);
            att.m_CurrentProxy.SetAPText(turn.ActionPoint);

            var stat = entity.GetAttribute<ActorStatAttribute>();
            if (stat == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({entity.RawName}) doesn\'t have {nameof(ActorStatAttribute)}.");
                return;
            }

            int
                fullHp = stat.GetOriginalValue<int>(att.m_HPNameHash),
                hp = stat.GetValue<int>(att.m_HPNameHash);

            att.m_CurrentProxy.SetHPFullText(fullHp);
            att.m_CurrentProxy.SetHPText(hp);
        }
    }
}