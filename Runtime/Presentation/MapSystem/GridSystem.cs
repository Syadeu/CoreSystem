using Syadeu.Database;
using Syadeu.Mono;
using System.Collections.Generic;

namespace Syadeu.Presentation.Map
{
    public sealed class GridSystem : PresentationSystemEntity<GridSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private EntitySystem m_EntitySystem;
        private RenderSystem m_RenderSystem;

        private KeyValuePair<SceneDataEntity, GridMapAttribute> m_MainGrid;
        private bool m_DrawGrid = false;

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

        private void M_EntitySystem_OnEntityCreated(IObject obj)
        {
            if (obj is SceneDataEntity sceneData)
            {
                var gridAtt = sceneData.GetAttribute<GridMapAttribute>();
                if (gridAtt == null) return;

                if (m_MainGrid.Value == null)
                {
                    m_MainGrid = new KeyValuePair<SceneDataEntity, GridMapAttribute>(sceneData, gridAtt);
                }
                else
                {
                    CoreSystem.Logger.Log(Channel.Entity,
                        $"Attempt to load grids more then one at SceneDataEntity({sceneData.Name}). This is not allowed.");
                }
            }
        }
        private void M_EntitySystem_OnEntityDestroy(IObject obj)
        {
            if (obj is SceneDataEntity sceneData)
            {
                if (m_MainGrid.Value.Equals(sceneData))
                {
                    m_MainGrid = new KeyValuePair<SceneDataEntity, GridMapAttribute>();
                }
            }
        }
        private void M_RenderSystem_OnRender()
        {
            if (m_DrawGrid && m_MainGrid.Value != null)
            {
                m_MainGrid.Value.Grid.DrawGL();
            }
        }
        #endregion
    }
}
