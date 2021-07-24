using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syadeu.Presentation
{
    public sealed class MapSystem : PresentationSystemEntity<MapSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private UnityEngine.GameObject m_MapEditorPrefab;
        private UnityEngine.GameObject m_MapEditorInstance;
        private ManagedGrid m_MainGrid;

        private SceneSystem m_SceneSystem;
        private EntitySystem m_EntitySystem;

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
            m_MapEditorPrefab = UnityEngine.Resources.Load<UnityEngine.GameObject>("MapEditor");
            CoreSystem.Logger.NotNull(m_MapEditorPrefab);

            CreateConsoleCommands();
            //UnityEngine.Object.Instantiate(m_MapEditorPrefab);

            return base.OnInitialize();
        }
        protected override PresentationResult OnInitializeAsync()
        {
            RequestSystem<SceneSystem>((other) => m_SceneSystem = other);
            RequestSystem<EntitySystem>((other) => m_EntitySystem = other);

            return base.OnInitializeAsync();
        }
        private void CreateConsoleCommands()
        {
            ConsoleWindow.CreateCommand((cmd) =>
            {
                if (m_MapEditorInstance != null)
                {
                    UnityEngine.Object.Destroy(m_MapEditorInstance);
                }
                m_MapEditorInstance = UnityEngine.Object.Instantiate(m_MapEditorPrefab);
            }, "open", "mapeditor");
            ConsoleWindow.CreateCommand((cmd) =>
            {
                if (m_MapEditorInstance != null)
                {
                    UnityEngine.Object.Destroy(m_MapEditorInstance);
                }
                m_MapEditorInstance = null;
            }, "close", "mapeditor");
        }
        #endregion

        public void LoadGrid(byte[] data)
        {
            m_MainGrid = ManagedGrid.FromBinary(data);
            ManagedCell[] cells = m_MainGrid.cells;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].GetValue() is EntityBase.Captured capturedEntity)
                {
                    m_EntitySystem.LoadEntity(capturedEntity);
                }
                else
                {

                }
            }
        }
        public void SaveGrid()
        {
            m_SceneSystem.CurrentSceneRef.m_SceneGridData = m_MainGrid.ToBinary();
        }
    }
}
