using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Map;
using Syadeu.Presentation.Proxy;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable
{
    [DisplayName("Attribute: TRPG GridCell UI")]
    public sealed class TRPGGridCellUIAttribute : AttributeBase,
        INotifyComponent<TRPGGridCellComponent>
    {

    }
    internal sealed class TRPGGridCellUIProcessor : AttributeProcessor<TRPGGridCellUIAttribute>,
        IAttributeOnProxy
    {
        public void OnProxyCreated(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            TRPGGridCellComponent component = entity.GetComponent<TRPGGridCellComponent>();
            if (component.m_GridPosition.index == -1) throw new System.Exception("index -1");

            monoObj.GetComponent<TRPGGridCellOverlayUI>().Initialize(component.m_GridPosition);
        }
        public void OnProxyRemoved(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
        }
    }
}