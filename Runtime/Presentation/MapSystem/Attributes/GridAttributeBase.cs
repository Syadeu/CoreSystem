using Newtonsoft.Json;
using Syadeu.Database;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    public abstract class GridAttributeBase : AttributeBase { }

    #region Grid Size Attribute
    public sealed class GridSizeAttribute : GridAttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "GridLocations")] public int2[] m_GridLocations;

        [JsonIgnore] public GridSystem GridSystem { get; internal set; }

        public GridSizeAttribute()
        {
            m_GridLocations = new int2[] { int2.zero };
        }
        protected override void OnDispose()
        {
            GridSystem = null;
        }

        public void UpdateGridCell()
        {
            GridSystem.UpdateGridEntity(Parent, GetCurrentGridCells());
        }
        private int[] GetCurrentGridCells()
        {
            GridSizeAttribute gridsize = Parent.GetAttribute<GridSizeAttribute>();
            if (gridsize == null)
            {
                throw new System.Exception();
            }
            if (GridSystem == null) throw new System.Exception();
            if (GridSystem.GridMap == null) throw new System.Exception();

            Entity<IEntity> entity = Parent;
            // TODO : 임시. 이후 gridsize 에 맞춰서 인덱스 반환
            int p0 = GridSystem.GridMap.Grid.GetCellIndex(entity.transform.position);

            return new int[] { p0 };
        }
    }
    //[Preserve]
    //internal sealed class GridSizeProcessor : AttributeProcessor<GridSizeAttribute>
    //{
    //    protected override void OnCreated(GridSizeAttribute attribute, EntityData<IEntityData> entity)
    //    {
    //        GridSystem gridSystem = PresentationSystem<GridSystem>.System;
    //        if (gridSystem == null) throw new System.Exception("System null");
    //        if (gridSystem.GridMap == null) throw new System.Exception("Grid null");

    //        gridSystem.UpdateGridEntity(entity, attribute.GetCurrentGridCells());
    //    }
    //}
    #endregion
}
