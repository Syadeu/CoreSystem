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

        private readonly Dictionary<SceneReference, List<SceneDataEntity>> m_SceneDataObjects = new Dictionary<SceneReference, List<SceneDataEntity>>();
        private readonly List<SceneDependence> m_SceneDependences = new List<SceneDependence>();

        private readonly List<SceneDataEntity> m_LoadedSceneData = new List<SceneDataEntity>();

        public IReadOnlyList<SceneDataEntity> LoadedSceneData => m_LoadedSceneData;

        private SceneSystem m_SceneSystem;
        private EntitySystem m_EntitySystem;
        private Render.RenderSystem m_RenderSystem;

        #region Presentation Methods
        protected override PresentationResult OnInitialize()
        {
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
                SceneDependence dependence = new SceneDependence
                {
                    m_SceneData = new Reference<SceneDataEntity>(sceneData[i])
                };
                SceneReference targetScene = sceneData[i].GetTargetScene();

                other.RegisterSceneLoadDependence(targetScene, dependence.RegisterOnSceneLoad);
                other.RegisterSceneUnloadDependence(targetScene, dependence.RegisterOnSceneLoad);

                m_SceneDependences.Add(dependence);

                CoreSystem.Logger.Log(Channel.Presentation,
                    $"Scene Data({sceneData[i].Name}) is registered.");
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

        }

        #endregion

        #region Inner Classes

        private struct SceneDependence
        {
            public Reference<SceneDataEntity> m_SceneData;
            private Hash m_InstanceHash;

            public void RegisterOnSceneLoad()
            {
                SceneDataEntity data = m_SceneData.GetObject();
                SceneReference targetScene = data.GetTargetScene();

                MapSystem mapSystem = PresentationSystem<MapSystem>.System;

                if (!mapSystem.m_SceneDataObjects.TryGetValue(targetScene, out var list))
                {
                    list = new List<SceneDataEntity>();
                    mapSystem.m_SceneDataObjects.Add(targetScene, list);
                }

                SceneDataEntity ins = (SceneDataEntity)mapSystem.m_EntitySystem.CreateObject(data.Hash);
                list.Add(ins);

                mapSystem.m_LoadedSceneData.Add(ins);

                m_InstanceHash = ins.Idx;
            }
            public void RegisterOnSceneUnload()
            {
                SceneDataEntity data = m_SceneData.GetObject();
                SceneReference targetScene = data.GetTargetScene();

                MapSystem mapSystem = PresentationSystem<MapSystem>.System;

                data.DestroyChildOnDestroy = false;
                mapSystem.m_EntitySystem.InternalDestroyEntity(data.Idx);

                mapSystem.m_LoadedSceneData.Remove(data);

                if (mapSystem.m_SceneDataObjects.TryGetValue(targetScene, out var list))
                {
                    var iter = list.Where(Predicate);
                    if (iter.Any())
                    {
                        list.Remove(iter.First());
                    }
                }
            }
            private bool Predicate(SceneDataEntity sceneData)
            {
                if (sceneData.Idx.Equals(m_InstanceHash)) return true;
                return false;
            }
        }

        #endregion
    }
}
