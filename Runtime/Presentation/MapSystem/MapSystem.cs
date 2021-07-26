using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using Syadeu.ThreadSafe;
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

        private readonly Dictionary<SceneReference, List<IObject>> m_SceneDataObjects = new Dictionary<SceneReference, List<IObject>>();

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
            RequestSystem<SceneSystem>((other) =>
            {
                SceneDataEntity[] sceneData = EntityDataList.Instance.m_Objects.Values
                    .Where((other) => (other is SceneDataEntity sceneData) && sceneData.m_BindScene && sceneData.IsValid())
                    .Select((other) => (SceneDataEntity)other)
                    .ToArray();

                for (int i = 0; i < sceneData.Length; i++)
                {
                    SceneDataEntity data = sceneData[i];
                    SceneReference targetScene = data.GetTargetScene();

                    other.RegisterSceneLoadDependence(targetScene, () =>
                    {
                        if (!m_SceneDataObjects.TryGetValue(targetScene, out var list))
                        {
                            list = new List<IObject>();
                            m_SceneDataObjects.Add(targetScene, list);
                        }

                        for (int i = 0; i < data.m_MapData.Length; i++)
                        {
                            list.Add(m_EntitySystem.CreateObject(data.m_MapData[i]));
                        }
                    });
                    CoreSystem.Logger.Log(Channel.Presentation,
                        $"Scene Data({data.Name}) is registered.");

                    other.RegisterSceneUnloadDependence(data.GetTargetScene(), () =>
                    {
                        if (m_SceneDataObjects.TryGetValue(targetScene, out var list))
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                MapDataEntity mapData = (MapDataEntity)list[i];
                                mapData.DestroyChildOnDestroy = false;

                                m_EntitySystem.DestroyObject(list[i].Idx);
                            }
                            list.Clear();
                        }
                    });
                }

                m_SceneSystem = other;
            });
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
            //m_SceneSystem.CurrentSceneRef.m_SceneGridData = m_MainGrid.ToBinary();
        }
    }
}
