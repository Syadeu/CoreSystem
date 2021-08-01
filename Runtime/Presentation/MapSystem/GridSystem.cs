using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syadeu.Presentation.Map
{
    public sealed class GridSystem : PresentationSystemEntity<GridSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => false;

        private EntitySystem m_EntitySystem;
        private RenderSystem m_RenderSystem;

        private KeyValuePair<SceneDataEntity, GridMapAttribute> m_MainGrid;
        private bool m_DrawGrid = false;

        private readonly ConcurrentQueue<GridSizeAttribute> m_WaitForRegister = new ConcurrentQueue<GridSizeAttribute>();

        private readonly Dictionary<Entity<IEntity>, int[]> m_EntityGridIndices = new Dictionary<Entity<IEntity>, int[]>();
        private readonly Dictionary<int, Entity<IEntity>> m_GridEntities = new Dictionary<int, Entity<IEntity>>();

        public GridMapAttribute GridMap => m_MainGrid.Value;

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
            CreateConsoleCommands();

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<EntitySystem>((other) =>
            {
                m_EntitySystem = other;
                m_EntitySystem.OnEntityCreated += M_EntitySystem_OnEntityCreated;
                m_EntitySystem.OnEntityDestroy += M_EntitySystem_OnEntityDestroy;
            });
            RequestSystem<RenderSystem>((other) =>
            {
                m_RenderSystem = other;
                m_RenderSystem.OnRender += M_RenderSystem_OnRender;
            });

            return base.OnInitializeAsync();
        }
        protected override PresentationResult OnPresentationAsync()
        {
            if (m_MainGrid.Value != null)
            {
                int waitForRegisterCount = m_WaitForRegister.Count;
                if (waitForRegisterCount > 0)
                {
                    for (int i = 0; i < waitForRegisterCount; i++)
                    {
                        if (!m_WaitForRegister.TryDequeue(out var att)) continue;

                        att.GridSystem = this;
                        UpdateGridEntity(att.Parent, att.GetCurrentGridCells());
                    }
                }
            }

            return base.OnPresentationAsync();
        }
        public override void Dispose()
        {
            m_EntitySystem.OnEntityCreated -= M_EntitySystem_OnEntityCreated;
            m_EntitySystem.OnEntityDestroy -= M_EntitySystem_OnEntityDestroy;
            m_RenderSystem.OnRender -= M_RenderSystem_OnRender;
            base.Dispose();
        }
        private void CreateConsoleCommands()
        {
            ConsoleWindow.CreateCommand((cmd) =>
            {
                m_DrawGrid = !m_DrawGrid;
            }, "draw", "grid");
        }

        private void M_EntitySystem_OnEntityCreated(EntityData<IEntityData> obj)
        {
            if (obj.Target is SceneDataEntity sceneData)
            {
                GridMapAttribute gridAtt = sceneData.GetAttribute<GridMapAttribute>();
                if (gridAtt == null) return;

                if (m_MainGrid.Value == null)
                {
                    m_MainGrid = new KeyValuePair<SceneDataEntity, GridMapAttribute>(sceneData, gridAtt);
                }
                else
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Attempt to load grids more then one at SceneDataEntity({sceneData.Name}). This is not allowed.");
                }
            }
            else if (obj.Target is EntityBase entity)
            {
                var gridSizeAtt = entity.GetAttribute<GridSizeAttribute>();
                if (gridSizeAtt == null) return;

                m_WaitForRegister.Enqueue(gridSizeAtt);
            }
        }
        private void M_EntitySystem_OnEntityDestroy(EntityData<IEntityData> obj)
        {
            if (obj.Target is SceneDataEntity sceneData)
            {
                if (m_MainGrid.Key.Equals(sceneData))
                {
                    m_MainGrid = new KeyValuePair<SceneDataEntity, GridMapAttribute>();
                }
            }
        }
        private void M_RenderSystem_OnRender()
        {
            if (m_DrawGrid && m_MainGrid.Value != null)
            {
                m_MainGrid.Value.Grid.DrawGL(.1f, m_RenderSystem.Camera);

                float sizeHalf = m_MainGrid.Value.Grid.cellSize * .5f;

                GL.PushMatrix();
                GridExtensions.DefaultMaterial.SetPass(0);
                Color color = Color.red;
                color.a = .5f;
                GL.Color(color);

                int[] gridEntities = m_GridEntities.Keys.ToArray();
                for (int i = 0; i < gridEntities.Length; i++)
                {
                    Vector3
                            cellPos = m_MainGrid.Value.Grid.GetCellPosition(gridEntities[i]),
                            p1 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf),
                            p2 = new Vector3(cellPos.x - sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                            p3 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z + sizeHalf),
                            p4 = new Vector3(cellPos.x + sizeHalf, cellPos.y + .1f, cellPos.z - sizeHalf);

                    GL.Vertex(p1);
                    GL.Vertex(p2);
                    GL.Vertex(p3);
                    GL.Vertex(p4);
                }

                GL.PopMatrix();
            }
        }
        #endregion

        public void UpdateGridEntity(Entity<IEntity> entity, int[] indices)
        {
            if (m_EntityGridIndices.TryGetValue(entity, out int[] cachedIndics))
            {
                for (int i = 0; i < cachedIndics.Length; i++)
                {
                    m_GridEntities.Remove(cachedIndics[i]);
                }
                m_EntityGridIndices.Remove(entity);
            }

            m_EntityGridIndices.Add(entity, indices);
            for (int i = 0; i < indices.Length; i++)
            {
                m_GridEntities.Add(indices[i], entity);
            }
        }
    }
}
