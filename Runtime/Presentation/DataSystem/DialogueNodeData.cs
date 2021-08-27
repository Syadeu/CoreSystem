using Newtonsoft.Json;
using Syadeu.Presentation.Actor;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Data
{
    public sealed class DialogueNodeData : DataObjectBase
    {
        [Serializable]
        public sealed class Logic
        {
            public bool m_EnableLogic = true;

            [Space]
            public IfPreviousSpeaker[] m_IfPreviousSpeaker = Array.Empty<IfPreviousSpeaker>();
            public IfPreviousText[] m_IfPreviousText = Array.Empty<IfPreviousText>();

            [Space]
            public int m_OptionIndex;

            public bool Predicate(DialogueNodeData prev, Option prevOption)
            {
                for (int i = 0; i < m_IfPreviousSpeaker.Length; i++)
                {
                    if (!m_IfPreviousSpeaker[i].m_Speaker.Equals(prev.m_Speaker)) return false;
                }
                for (int i = 0; i < m_IfPreviousText.Length; i++)
                {
                    if (!m_IfPreviousText[i].m_TextData.Equals(prevOption.m_TextData)) return false;
                    if (!m_IfPreviousText[i].m_TextIndex.Equals(prevOption.m_TextIndex)) return false;
                }
                return true;
            }
        }
        [Serializable]
        public sealed class IfPreviousSpeaker
        {
            [JsonProperty(Order = 0, PropertyName = "Speaker")] public Reference<ActorEntity> m_Speaker;
        }
        [Serializable]
        public sealed class IfPreviousText
        {
            [JsonProperty(Order = 0, PropertyName = "TextData")] public Reference<LocalizedTextData> m_TextData;
            [JsonProperty(Order = 1, PropertyName = "TextIndex")] public int m_TextIndex;
        }

        [Serializable]
        public sealed class Option
        {
            [JsonProperty(Order = 0, PropertyName = "TextData")] public Reference<LocalizedTextData> m_TextData;
            [JsonProperty(Order = 1, PropertyName = "TextIndex")] public int m_TextIndex;
        }

        [JsonProperty(Order = 0, PropertyName = "Speaker")] public Reference<ActorEntity> m_Speaker;
        [JsonProperty(Order = 1, PropertyName = "Logics")] public Logic[] m_Logics = Array.Empty<Logic>();
        [JsonProperty(Order = 2, PropertyName = "Options")] public Option[] m_Options = Array.Empty< Option>();
        
        public bool Predicate(DialogueNodeData prev, Option prevOption)
        {
            for (int i = 0; i < m_Logics.Length; i++)
            {
                if (!m_Logics[i].m_EnableLogic) continue;

                Logic logic = m_Logics[i];
                if (!logic.Predicate(prev, prevOption)) return false;
            }
            return true;
        }
    }
}
