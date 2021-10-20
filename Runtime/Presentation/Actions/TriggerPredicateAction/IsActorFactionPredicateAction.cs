using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("PredicateAction: Is Actor Faction ?")]
    public sealed class IsActorFactionPredicateAction : TriggerPredicateAction
    {
        public enum PredicateType
        {
            /// <summary>
            /// <see cref="m_Value"/> 에서 정한게 하나라도 맞으면 true
            /// </summary>
            True,
            False
        }

        [JsonProperty(Order = 0, PropertyName = "PredicateType")]
        private PredicateType m_PredicateType = PredicateType.True;

        [JsonProperty(Order = 1, PropertyName = "Value")]
        private Reference<ActorFaction>[] m_Value = Array.Empty<Reference<ActorFaction>>();

        protected override bool OnExecute(EntityData<IEntityData> entity)
        {
#if DEBUG
            if (!entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"An invalid entity trying to execute {nameof(IsActorFactionPredicateAction)}.");
                return false;
            }
            else if (!(entity.Target is ActorEntity))
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"This entity({entity.Name}) is not a {nameof(ActorEntity)} but trying to predicate({nameof(IsActorFactionPredicateAction)})");
                return false;
            }
            else if (!entity.HasComponent<ActorFactionComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Action,
                    $"This entity({entity.RawName}) dosen\'t have any {nameof(ActorFactionComponent)} but you\'re trying to predicate. This is not allowed.");

                return false;
            }
#endif
            ActorFactionComponent actorFaction = entity.GetComponentReadOnly<ActorFactionComponent>();

            for (int i = 0; i < m_Value.Length; i++)
            {
                if (m_Value[i].Hash.Equals(actorFaction.FactionID))
                {
                    if (m_PredicateType == PredicateType.True) return true;
                    return false;
                }
            }
            return m_PredicateType == PredicateType.False;
        }
    }
}
