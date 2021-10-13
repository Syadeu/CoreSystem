using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Proxy;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: TRPG GridCell UI")]
    public sealed class TRPGGridCellUIAttribute : AttributeBase
    {

    }
    internal sealed class TRPGGridCellUIProcessor : AttributeProcessor<TRPGGridCellUIAttribute>,
        IAttributeOnProxy
    {
        public void OnProxyCreated(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            GridCellComponent component = entity.GetComponent<GridCellComponent>();
            if (component.m_GridPosition.index == -1) throw new System.Exception("index -1");

            monoObj.GetComponent<TRPGGridCellOverlayUI>().Initialize(component.m_GridPosition, component.m_IsDetectionCell);
        }
        public void OnProxyRemoved(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
        }
    }
}