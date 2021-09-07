using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Data;
using System;
using System.ComponentModel;

namespace Syadeu.Presentation.Map
{
    [DisplayName("Data: Terrain Data")]
    public sealed class TerrainData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "Data")]
        public PrefabReference<UnityEngine.TerrainData>[] m_Data = Array.Empty<PrefabReference<UnityEngine.TerrainData>>();
    }
}
