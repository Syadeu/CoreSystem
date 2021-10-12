using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.BehaviorTree
{
    public static class PresentationBehaviorTreeUtility
    {
        public const string c_SelfEntityString = "This";

        public static TaskStatus LoadAttributeFromMono<T>(SharedRecycleableMonobehaviour target, string calledFrom, out T att) where T : AttributeBase
        {
            if (!target.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {calledFrom}.");
                att = null;
                return TaskStatus.Failure;
            }

            att = target.GetAttribute<T>();
            if (att == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {calledFrom}. {nameof(T)} not found.");
                return TaskStatus.Failure;
            }

            return TaskStatus.Success;
        }

        public static bool IsValid(this SharedRecycleableMonobehaviour other)
        {
            if (other.Value == null || !other.Value.Activated || !other.Value.entity.IsValid()) return false;

            return true;
        }
        public static bool IsValid(this SharedEntity other)
        {
            if (!other.Value.IsValid()) return false;

            return true;
        }

        public static Entity<IEntity> GetEntity(this SharedRecycleableMonobehaviour other)
        {
            if (!other.IsValid()) return Entity<IEntity>.Empty;

            return other.Value.entity;
        }

        public static T GetAttribute<T>(this SharedRecycleableMonobehaviour other)
            where T : AttributeBase
        {
            if (!other.IsValid()) return null;

            T att = other.Value.entity.GetAttribute<T>();
            return att;
        }
        public static T GetComponent<T>(this SharedRecycleableMonobehaviour other)
            where T : unmanaged, IEntityComponent
        {
            if (!other.IsValid()) return default(T);

            T att = other.Value.entity.GetComponent<T>();
            return att;
        }
        public static bool HasComponent<T>(this SharedRecycleableMonobehaviour other)
            where T : unmanaged, IEntityComponent
        {
            if (!other.IsValid()) return false;

            bool att = other.Value.entity.HasComponent<T>();
            return att;
        }
        public static T GetAttribute<T>(this SharedEntity other)
            where T : AttributeBase
        {
            if (!other.IsValid()) return null;

            T att = other.Value.GetAttribute<T>();
            return att;
        }

        public static Instance<T> GetProvider<T>(this LoadActorControllerAttribute other)
            where T : ActorProviderBase
        {
            if (other == null) return Instance<T>.Empty;
            return other.ActorController.Parent.GetComponent<ActorControllerComponent>().GetProvider<T>();
        }

        public static T GetComponent<T>(this SharedEntity other)
            where T : unmanaged, IEntityComponent
        {
            if (!other.IsValid()) return default(T);

            T component = other.Value.GetComponent<T>();
            return component;
        }
        public static T AddComponent<T>(this SharedEntity other, T component)
            where T : unmanaged, IEntityComponent
        {
            if (!other.IsValid()) return default(T);

            return other.Value.AddComponent(component);
        }
        public static bool HasComponent<T>(this SharedEntity other)
            where T : unmanaged, IEntityComponent
        {
            if (!other.IsValid()) return false;

            return other.Value.HasComponent<T>();
        }
    }
}
