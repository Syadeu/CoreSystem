using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Syadeu.Collections.Graphs
{
    [Serializable]
    public sealed class VisualGraphField
    {
        [SerializeField, JsonProperty(PropertyName = "VisualGraph")]
        private VisualGraph m_VisualGraph;

        [JsonIgnore]
        public VisualGraph VisualGraph => m_VisualGraph;

        public VisualGraphField()
        {
            m_VisualGraph = null;
        }
        public VisualGraphField(VisualGraph graph)
        {
            m_VisualGraph = graph;
        }
    }
}
