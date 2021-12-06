// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
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

        //private readonly Dictionary<SceneReference, List<EntityData<SceneDataEntity>>> m_SceneDataObjects = new Dictionary<SceneReference, List<EntityData<SceneDataEntity>>>();
        private readonly List<SceneDependence> m_SceneDependences = new List<SceneDependence>();

        //private readonly List<EntityData<SceneDataEntity>> m_LoadedSceneData = new List<EntityData<SceneDataEntity>>();

        //public IReadOnlyList<EntityData<SceneDataEntity>> LoadedSceneData => m_LoadedSceneData;

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
            RequestSystem<DefaultPresentationGroup, SceneSystem>(Bind);
            RequestSystem<DefaultPresentationGroup, EntitySystem>(Bind);
            RequestSystem<DefaultPresentationGroup, Render.RenderSystem>(Bind);

            return base.OnInitializeAsync();
        }

        #region Bind
        private void Bind(SceneSystem other)
        {
            m_SceneSystem = other;

            SceneDataEntity[] sceneData = EntityDataList.Instance.m_Objects.Values
                    .Where(other => (other is SceneDataEntity sceneData) && sceneData.m_BindScene && sceneData.IsValid())
                    .Select(other => (SceneDataEntity)other)
                    .ToArray();

            for (int i = 0; i < sceneData.Length; i++)
            {
                SceneDependence dependence = new SceneDependence
                {
                    m_SceneData = new Reference<SceneDataEntity>(sceneData[i])
                };
                SceneReference targetScene = sceneData[i].GetTargetScene();

                other.RegisterSceneLoadDependence(targetScene, dependence.RegisterOnSceneLoad);
                other.RegisterSceneUnloadDependence(targetScene, dependence.RegisterOnSceneUnload);

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

        private sealed class SceneDependence
        {
            public Reference<SceneDataEntity> m_SceneData;
            private EntityData<SceneDataEntity> m_InstanceHash;

            public ICustomYieldAwaiter RegisterOnSceneLoad()
            {
                SceneDataEntity data = m_SceneData.GetObject();
                SceneReference targetScene = data.GetTargetScene();

                MapSystem mapSystem = PresentationSystem<DefaultPresentationGroup, MapSystem>.System;

                //if (!mapSystem.m_SceneDataObjects.TryGetValue(targetScene, out var list))
                //{
                //    list = new List<EntityData<SceneDataEntity>>();
                //    mapSystem.m_SceneDataObjects.Add(targetScene, list);
                //}

                var ins = mapSystem.m_EntitySystem.CreateObject(data.Hash);
                EntityData<SceneDataEntity> entity = ins.Cast<IEntityData, SceneDataEntity>();
                //list.Add(entity);

                //mapSystem.m_LoadedSceneData.Add(entity);

                m_InstanceHash = entity;

                return data.LoadAllAssets();
            }
            public void RegisterOnSceneUnload()
            {
                if (m_InstanceHash.IsEmpty()) return;

                SceneDataEntity data = m_InstanceHash.Target;
                SceneReference targetScene = data.GetTargetScene();

                MapSystem mapSystem = PresentationSystem<DefaultPresentationGroup, MapSystem>.System;

                m_InstanceHash.Destroy();
                m_InstanceHash = EntityData<SceneDataEntity>.Empty;
                //mapSystem.m_EntitySystem.InternalDestroyEntity(data.Idx);

                //mapSystem.m_LoadedSceneData.Remove(data);

                //if (mapSystem.m_SceneDataObjects.TryGetValue(targetScene, out var list))
                //{
                //    var iter = list.Where(Predicate);
                //    if (iter.Any())
                //    {
                //        list.Remove(iter.First());
                //    }
                //}
            }
            private bool Predicate(EntityData<SceneDataEntity> sceneData)
            {
                if (sceneData.Idx.Equals(m_InstanceHash)) return true;
                return false;
            }
        }

        #endregion
    }
}
