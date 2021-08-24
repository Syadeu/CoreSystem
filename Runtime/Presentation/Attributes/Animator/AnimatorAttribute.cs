using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class AnimatorAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "TriggerActions")]
        public Reference<AnimationTriggerAction>[] m_TriggerActions = Array.Empty<Reference<AnimationTriggerAction>>();

        [JsonIgnore] public AnimatorComponent Animator { get; internal set; }
        [JsonIgnore] public Dictionary<Hash, List<Reference<AnimationTriggerAction>>> Actions { get; internal set; }
    }
    internal sealed class AnimatorProcessor : AttributeProcessor<AnimatorAttribute>,
        IAttributeOnProxy
    {
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
            //att.Animator.get
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
