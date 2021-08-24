using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class AnimatorAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "TriggerActions")]
        public Reference<AnimationTriggerAction>[] m_TriggerActions = Array.Empty<Reference<AnimationTriggerAction>>();

        [Space]
        [JsonProperty(Order = 10, PropertyName = "OnMoveActions")]
        public Reference<ActionBase>[] m_OnMoveActions = Array.Empty<Reference<ActionBase>>();

        [JsonIgnore] internal AnimatorComponent Animator { get; set; }
        [JsonIgnore] public Dictionary<Hash, List<Reference<AnimationTriggerAction>>> Actions { get; internal set; }

        public void SetFloat(int key, float value)
        {
            if (Animator == null) return;

            Animator.m_Animator.SetFloat(key, value);
        }
    }
    internal sealed class AnimatorProcessor : AttributeProcessor<AnimatorAttribute>,
        IAttributeOnProxy
    {
        protected override void OnInitialize()
        {
            EventSystem.AddEvent<OnMoveStateChangedEvent>(OnMoveStateChangedEventHandler);
        }
        private void OnMoveStateChangedEventHandler(OnMoveStateChangedEvent ev)
        {
            AnimatorAttribute att = ev.Entity.GetAttribute<AnimatorAttribute>();
            if (att == null) return;

            var entity = ev.Entity.As<IEntity, IEntityData>();
            for (int i = 0; i < att.m_OnMoveActions.Length; i++)
            {
                att.m_OnMoveActions[i].Execute(entity);
            }
        }

        protected override void OnCreated(AnimatorAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.Actions = new Dictionary<Hash, List<Reference<AnimationTriggerAction>>>();
            for (int i = 0; i < attribute.m_TriggerActions.Length; i++)
            {
                AnimationTriggerAction temp = attribute.m_TriggerActions[i].GetObject();
                Hash hash = Hash.NewHash(temp.m_TriggerName);

                if (!attribute.Actions.TryGetValue(hash, out List<Reference<AnimationTriggerAction>> list))
                {
                    list = new List<Reference<AnimationTriggerAction>>();
                    attribute.Actions.Add(hash, list);
                }
                list.Add(attribute.m_TriggerActions[i]);
            }
        }
        protected override void OnDestroy(AnimatorAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.Actions = null;
        }

        private static bool Setup(AnimatorAttribute att, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            att.Animator = monoObj.GetComponent<AnimatorComponent>();
            if (att.Animator == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) has {nameof(AnimatorAttribute)} but cannot found animator");
                return false;
            }
            att.Animator.m_AnimatorAttribute = att;

            return true;
        }
        public void OnProxyCreated(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            AnimatorAttribute att = (AnimatorAttribute)attribute;
            if (!Setup(att, entity, monoObj))
            {
                return;
            }

            att.Animator.SetActive(true);
        }

        public void OnProxyRemoved(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            AnimatorAttribute att = (AnimatorAttribute)attribute;

            att.Animator.SetActive(false);
            att.Animator.m_AnimatorAttribute = null;
            att.Animator = null;
        }
    }
}
