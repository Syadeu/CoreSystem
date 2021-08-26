using Newtonsoft.Json;
using System;

namespace Syadeu.Presentation.Data
{
    public abstract class DataObjectBase : ObjectBase { }

    public sealed class LocalizedTextData : DataObjectBase
    {
        public enum Culture
        {
            English,
            Korean
        }

        [Serializable]
        public sealed class Text
        {
            [JsonProperty(Order = 0, PropertyName = "Culture")] public Culture m_Culture;
            [JsonProperty(Order = 1, PropertyName = "Text")] public string m_Text;
        }
        [Serializable]
        public sealed class Entry
        {
            [JsonProperty(Order = 0, PropertyName = "Texts")] public Text[] m_Texts = Array.Empty<Text>();
        }

        [JsonProperty(Order = 0, PropertyName = "Entries")] public Entry[] m_Entries = Array.Empty<Entry>();
    }
    public sealed class DialogueData : DataObjectBase
    {
        public sealed class Entry
        {

        }
    }
}
