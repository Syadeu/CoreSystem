using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using UnityEngine;

namespace Syadeu.Presentation.Attributes
{
    [DisplayName("Attribute: Animator")]
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class AnimatorAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "AnimationTrigger")]
        public Reference<AnimationTriggerAction>[] m_AnimationTriggers = Array.Empty<Reference<AnimationTriggerAction>>();

        [JsonIgnore] internal AnimatorComponent Animator { get; set; }
        [JsonIgnore] internal Dictionary<int, object> Parameters { get; set; }
        [JsonIgnore] public Dictionary<Hash, List<Reference<AnimationTriggerAction>>> AnimationTriggers { get; internal set; }

        public void SetFloat(int key, float value)
        {
            if (Animator == null) return;
            Animator.m_Animator.SetFloat(key, value);
        }
        public void SetFloat(string key, float value)
        {
            if (Animator == null) return;
            Animator.m_Animator.SetFloat(key, value);
        }
        public float GetFloat(int key)
        {
            if (Animator == null) return 0;
            return Animator.m_Animator.GetFloat(key);
        }
    }
    internal sealed class AnimatorProcessor : AttributeProcessor<AnimatorAttribute>,
        IAttributeOnProxy
    {
        protected override void OnCreated(AnimatorAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.Parameters = new Dictionary<int, object>();
            attribute.AnimationTriggers = new Dictionary<Hash, List<Reference<AnimationTriggerAction>>>();
            for (int i = 0; i < attribute.m_AnimationTriggers.Length; i++)
            {
                AnimationTriggerAction temp = attribute.m_AnimationTriggers[i].GetObject();
                Hash hash = Hash.NewHash(temp.m_TriggerName);

                if (!attribute.AnimationTriggers.TryGetValue(hash, out List<Reference<AnimationTriggerAction>> list))
                {
                    list = new List<Reference<AnimationTriggerAction>>();
                    attribute.AnimationTriggers.Add(hash, list);
                }
                list.Add(attribute.m_AnimationTriggers[i]);
            }
        }
        protected override void OnDestroy(AnimatorAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.Parameters = null;
            attribute.AnimationTriggers = null;
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
