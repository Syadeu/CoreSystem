using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Internal;
using Syadeu.ThreadSafe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syadeu.Presentation.Map
{
    public sealed class MapSystem : PresentationSystemEntity<MapSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private UnityEngine.GameObject m_MapEditorPrefab;
        private UnityEngine.GameObject m_MapEditorInstance;
        private ManagedGrid m_MainGrid;

        private readonly Dictionary<SceneReference, List<SceneDataEntity>> m_SceneDataObjects = new Dictionary<SceneReference, List<SceneDataEntity>>();

        private SceneSystem m_SceneSystem;
        private EntitySystem m_EntitySystem;
        private RenderSystem m_RenderSystem;

        private bool m_DrawGrid = false;
        private bool m_Disposed = false;

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
                            list = new List<SceneDataEntity>();
                            m_SceneDataObjects.Add(targetScene, list);
                        }

                        list.Add((SceneDataEntity)m_EntitySystem.CreateObject(data.Hash));
                    });

                    other.RegisterSceneUnloadDependence(targetScene, () =>
                    {
                        if (m_SceneDataObjects.TryGetValue(targetScene, out var list))
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                SceneDataEntity data = list[i];
                                data.DestroyChildOnDestroy = false;
                                m_EntitySystem.DestroyObject(list[i].Idx);
                            }
                            list.Clear();

                            m_SceneDataObjects.Remove(targetScene);
                        }
                    });

                    CoreSystem.Logger.Log(Channel.Presentation,
                        $"Scene Data({data.Name}) is registered.");
                }

                m_SceneSystem = other;
            });
            RequestSystem<EntitySystem>((other) => m_EntitySystem = other);
            RequestSystem<RenderSystem>((other) =>
            {
                m_RenderSystem = other;

                m_RenderSystem.OnRender += M_RenderSystem_OnRender;
            });

            return base.OnInitializeAsync();
        }
        public override void Dispose()
        {
            m_RenderSystem.OnRender -= M_RenderSystem_OnRender;

            m_Disposed = true;
            base.Dispose();
        }

        private void M_RenderSystem_OnRender()
        {
            if (m_DrawGrid)
            {
                foreach (var item in m_SceneDataObjects)
                {
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        var gridAtt = item.Value[i].GetAttribute<GridMapAttribute>();
                        if (gridAtt == null) continue;

                        // TODO : 임시 코드
                        gridAtt.Grid.DrawGL();
                    }
                }
            }
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
            ConsoleWindow.CreateCommand((cmd) =>
            {
                m_DrawGrid = !m_DrawGrid;
            }, "draw", "grid");
        }
        #endregion

        public IReadOnlyList<SceneDataEntity> GetCurrentSceneData()
        {
            if (m_SceneDataObjects.TryGetValue(m_SceneSystem.CurrentSceneRef, out var list))
            {
                return list;
            }
            return Array.Empty<SceneDataEntity>();
        }
    }
}
