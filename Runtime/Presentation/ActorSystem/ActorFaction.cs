using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Faction")]
    public sealed class ActorFaction : DataObjectBase
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [JsonProperty(Order = 0, PropertyName = "FactionType")]
        internal FactionType m_FactionType = FactionType.Player;

        [JsonProperty(Order = 1, PropertyName = "Allies")]
        internal Reference<ActorFaction>[] m_Allies = Array.Empty<Reference<ActorFaction>>();
        [JsonProperty(Order = 2, PropertyName = "Enemies")]
        internal Reference<ActorFaction>[] m_Enemies = Array.Empty<Reference<ActorFaction>>();
#pragma warning restore IDE0044 // Add readonly modifier

        [JsonIgnore] public FactionType FactionType => m_FactionType;

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Instance<ActorFaction>>();
            AotHelper.EnsureType<InstanceArray<ActorFaction>>();
            AotHelper.EnsureList<Instance<ActorFaction>>();

            AotHelper.EnsureType<Reference<ActorFaction>>();
            AotHelper.EnsureList<Reference<ActorFaction>>();
            AotHelper.EnsureType<ActorFaction>();
            AotHelper.EnsureList<ActorFaction>();
        }
    }
}
