using Newtonsoft.Json;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;

namespace Syadeu.Presentation.Data
{
    public sealed class DialogueData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Dialogues")] public Reference<DialogueNodeData>[] m_Dialogues = Array.Empty<Reference<DialogueNodeData>>();

        public DialogueHandler Start(LocalizedTextData.Culture culture, params Entity<ActorEntity>[] entries)
        {
            return new DialogueHandler(culture, entries);
        }
    }
}
