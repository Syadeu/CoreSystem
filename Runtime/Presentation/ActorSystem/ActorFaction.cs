using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Attributes;
using System;

namespace Syadeu.Presentation.Actor
{
    [Serializable, AttributeAcceptOnly]
    public sealed class ActorFaction : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Allies")] private Reference<ActorFaction>[] m_Allies;
        [JsonProperty(Order = 1, PropertyName = "Enemies")] private Reference<ActorFaction>[] m_Enemies;
    }
}
