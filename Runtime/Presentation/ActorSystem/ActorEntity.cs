using Newtonsoft.Json;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    public sealed class ActorEntity : EntityBase
    {
        [JsonIgnore] internal ActorSystem m_ActorSystem;

        [JsonProperty(Order = 0, PropertyName = "Faction")] private Reference<ActorFaction> m_Faction;

        [JsonIgnore] public ActorFaction Faction => m_Faction.IsValid() ? m_Faction.GetObject() : null;
    }
}
