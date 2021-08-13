﻿using Syadeu.Database;
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

        private readonly Dictionary<SceneReference, List<SceneDataEntity>> m_SceneDataObjects = new Dictionary<SceneReference, List<SceneDataEntity>>();

        private SceneSystem m_SceneSystem;
        private EntitySystem m_EntitySystem;
        private Render.RenderSystem m_RenderSystem;

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
            RequestSystem<SceneSystem>(Bind);
            RequestSystem<EntitySystem>(Bind);
            RequestSystem<Render.RenderSystem>(Bind);

            return base.OnInitializeAsync();
        }

        #region Bind
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;

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

                    SceneDataEntity ins = (SceneDataEntity)m_EntitySystem.CreateObject(data.Hash);
                    list.Add(ins);
                });

                other.RegisterSceneUnloadDependence(targetScene, () =>
                {
                    if (m_SceneDataObjects.TryGetValue(targetScene, out var list))
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            SceneDataEntity data = list[i];
                            data.DestroyChildOnDestroy = false;
                            m_EntitySystem.InternalDestroyEntity(list[i].Idx);
                        }
                        list.Clear();

                        m_SceneDataObjects.Remove(targetScene);
                    }
                });

                CoreSystem.Logger.Log(Channel.Presentation,
                    $"Scene Data({data.Name}) is registered.");
            }
        }
        private void Bind(EntitySystem other)
        {
            m_EntitySystem = other;
        }
        private void Bind(Render.RenderSystem other)
        {
            m_RenderSystem = other;
        }

        #endregion

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
