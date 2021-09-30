using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    /// <summary>
    /// <see cref="TRPGActionPointOverlayUI"/>
    /// </summary>
    public sealed class TRPGActorActionPointOverlayUIProvider : ActorOverlayUIProvider
    {
        [Header("GridCell")]
        [JsonProperty(Order = 0, PropertyName = "GridCellPrefab")]
        private Reference<UIObjectEntity> m_GridCellPrefab = Reference<UIObjectEntity>.Empty;
    }
}