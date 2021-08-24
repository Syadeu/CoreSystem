using Newtonsoft.Json;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.Attributes
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public sealed class AnimatorAttribute : AttributeBase
    {
        [JsonIgnore] public AnimatorComponent Animator { get; internal set; }
    }
    internal sealed class AnimatorProcessor : AttributeProcessor<AnimatorAttribute>,
        IAttributeOnProxy
    {

        private static bool Setup(AnimatorAttribute att, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            att.Animator = monoObj.GetComponent<AnimatorComponent>();
            if (att.Animator == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) has {nameof(AnimatorAttribute)} but cannot found animator");
                return false;
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

            att.Animator.SetActive(true);
            //att.Animator.get
        }

        public void OnProxyRemoved(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            AnimatorAttribute att = (AnimatorAttribute)attribute;

            att.Animator.SetActive(false);
            att.Animator = null;
        }
    }
}
