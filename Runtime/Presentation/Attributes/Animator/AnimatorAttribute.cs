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

        [JsonIgnore] internal AnimatorComponent AnimatorComponent { get; set; }
        [JsonIgnore] internal Dictionary<int, object> Parameters { get; set; }
        [JsonIgnore] public Dictionary<Hash, List<Reference<AnimationTriggerAction>>> AnimationTriggers { get; internal set; }

        public void SetInteger(int key, int value)
        {
            Parameters[key] = value;
            if (AnimatorComponent != null) AnimatorComponent.m_Animator.SetInteger(key, value);
        }
        public int GetInteger(int key) => (int)Parameters[key];

        public void SetFloat(int key, float value)
        {
            Parameters[key] = value;
            if (AnimatorComponent != null) AnimatorComponent.m_Animator.SetFloat(key, value);
        }
        public void SetFloat(string key, float value) => SetFloat(Animator.StringToHash(key), value);
        public float GetFloat(int key) => (float)Parameters[key];

        public void SetBool(int key, bool value)
        {
            Parameters[key] = value;
            if (AnimatorComponent != null) AnimatorComponent.m_Animator.SetBool(key, value);
        }
        public bool GetBool(int key) => (bool)Parameters[key];

        public void SetTrigger(int key)
        {
            if (AnimatorComponent == null) return;
            AnimatorComponent.m_Animator.SetTrigger(key);
        }
    }
    internal sealed class AnimatorProcessor : AttributeProcessor<AnimatorAttribute>,
        IAttributeOnProxy
    {
        protected override void OnCreated(AnimatorAttribute attribute, EntityData<IEntityData> entity)
        {
            attribute.Parameters = new Dictionary<int, object>();
            Animator originAnimator = ((GameObject)((ProxyTransform)entity.As<IEntityData, IEntity>().transform).prefab.Asset).GetComponentInChildren<Animator>();
            for (int i = 0; i < originAnimator.parameterCount; i++)
            {
                var param = originAnimator.GetParameter(i);
                switch (param.type)
                {
                    case AnimatorControllerParameterType.Float:
                        attribute.Parameters.Add(param.nameHash, originAnimator.GetFloat(param.nameHash));
                        break;
                    case AnimatorControllerParameterType.Int:
                        attribute.Parameters.Add(param.nameHash, originAnimator.GetInteger(param.nameHash));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        attribute.Parameters.Add(param.nameHash, originAnimator.GetBool(param.nameHash));
                        break;
                    default:
                        break;
                }
            }

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
            att.AnimatorComponent = monoObj.GetComponent<AnimatorComponent>();
            if (att.AnimatorComponent == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) has {nameof(AnimatorAttribute)} but cannot found animator");
                return false;
            }
            att.AnimatorComponent.m_AnimatorAttribute = att;

            foreach (var item in att.Parameters)
            {
                if (item.Value is int integer) att.AnimatorComponent.m_Animator.SetInteger(item.Key, integer);
                else if (item.Value is float single) att.AnimatorComponent.m_Animator.SetFloat(item.Key, single);
                else if (item.Value is bool boolen) att.AnimatorComponent.m_Animator.SetBool(item.Key, boolen);
            }

            return true;
        }
        public void OnProxyCreated(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            AnimatorAttribute att = (AnimatorAttribute)attribute;
            if (!Setup(att, entity, monoObj))
            {
                return;
            }

            att.AnimatorComponent.SetActive(true);
        }

        public void OnProxyRemoved(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            AnimatorAttribute att = (AnimatorAttribute)attribute;

            att.AnimatorComponent.SetActive(false);
            att.AnimatorComponent.m_AnimatorAttribute = null;
            att.AnimatorComponent = null;
        }
    }
}
