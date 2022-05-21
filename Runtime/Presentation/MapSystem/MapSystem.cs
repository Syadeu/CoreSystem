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
using Syadeu.Collections.Buffer.LowLevel;
using Syadeu.Mono;
using Syadeu.Presentation.Components;
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
    public sealed class MapSystem : PresentationSystemEntity<MapSystem>,
        INotifySystemModule<SpawnMapDataModule>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly List<SceneDependence> m_SceneDependences = new List<SceneDependence>();

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

                CoreSystem.Logger.Log(LogChannel.Presentation,
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

        public void AddSpawnEntity(params SpawnMapDataEntity.Point[] points)
        {
            GetModule<SpawnMapDataModule>().AddSpawnEntity(points);
        }
        public void RemoveSpawnEntity(params SpawnMapDataEntity.Point[] points)
        {
            GetModule<SpawnMapDataModule>().RemoveSpawnEntity(points);
        }

        #region Inner Classes

        private sealed class SceneDependence
        {
            public Reference<SceneDataEntity> m_SceneData;
            private Entity<SceneDataEntity> m_InstanceHash;

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

                var ins = mapSystem.m_EntitySystem.CreateEntity(new Reference(data.Hash));
                Entity<SceneDataEntity> entity = ins.ToEntity<SceneDataEntity>();
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
                m_InstanceHash = Entity<SceneDataEntity>.Empty;
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
            private bool Predicate(Entity<SceneDataEntity> sceneData)
            {
                if (sceneData.Idx.Equals(m_InstanceHash)) return true;
                return false;
            }
        }

        #endregion
    }

    internal sealed class SpawnMapDataModule : PresentationSystemModule<MapSystem>
    {
        private Dictionary<Hash, Entry> m_Entries = new Dictionary<Hash, Entry>();

        private CoroutineSystem m_CoroutineSystem;

        protected override void OnInitialize()
        {
            SpawnedEntityComponentProcessor.s_Module = this;

            RequestSystem<DefaultPresentationGroup, CoroutineSystem>(Bind);
        }
        protected override void OnDispose()
        {
            SpawnedEntityComponentProcessor.s_Module = null;

            m_CoroutineSystem = null;
        }

        private void Bind(CoroutineSystem other)
        {
            m_CoroutineSystem = other;
        }

        public void AddSpawnEntity(params SpawnMapDataEntity.Point[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Entry entry = new Entry(points[i]);
                m_Entries.Add(entry.Hash, entry);

                entry.StartUpdate(m_CoroutineSystem);
            }
        }
        public void RemoveSpawnEntity(params SpawnMapDataEntity.Point[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Hash hash = points[i].GetHash();
                var entry = GetEntry(hash);
#if DEBUG_MODE
                if (entry == null) continue;
#endif

                entry.Dispose();
                m_Entries.Remove(hash);
            }
        }

        private Entry GetEntry(Hash hash)
        {
#if DEBUG_MODE
            if (!m_Entries.ContainsKey(hash))
            {
                CoreSystem.Logger.LogError(LogChannel.Presentation,
                    $"??");

                return null;
            }
#endif
            return m_Entries[hash];
        }

        private sealed class Entry : IDisposable
        {
            private SpawnMapDataEntity.Point m_Point;
            private List<InstanceID> m_Instances = new List<InstanceID>();

            public Hash Hash => m_Point.GetHash();

            public Entry(SpawnMapDataEntity.Point p)
            {
                m_Point = p;

                if (m_Point.m_SpawnAtStart)
                {
                    Spawn(1);
                }
            }

            public void Spawn(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Entity<EntityBase> entity = m_Point.m_TargetEntity.CreateEntity();

                    entity.AddComponent<SpawnedEntityComponent>();
                    ref SpawnedEntityComponent com = ref entity.GetComponent<SpawnedEntityComponent>();
                    com.m_Hash = Hash;

                    m_Instances.Add(entity);
                }
            }
            public void Remove(InstanceID entity)
            {
                m_Instances.Remove(entity);
            }

            CoroutineHandler m_Handle;
            public void StartUpdate(CoroutineSystem coroutineSystem)
            {
                m_Handle = coroutineSystem.StartCoroutine(new UpdateJob
                {
                    m_Entry = this
                });
            }
            private sealed class UpdateJob : ICoroutineJob
            {
                public Entry m_Entry;
                public UpdateLoop Loop => default;

                public void Dispose()
                {
                    m_Entry = null;
                }
                public IEnumerator Execute()
                {
                    if (m_Entry.m_Point.m_EnableAutoSpawn)
                    {
                        Collections.Timer timer = Collections.Timer.Start();
                        while (true)
                        {
                            if (timer.IsExceeded(m_Entry.m_Point.m_PerTime))
                            {
                                m_Entry.Spawn(1);

                                timer = Collections.Timer.Start();
                            }

                            yield return null;
                        }
                    }
                    yield break;
                }
            }

            public void Dispose()
            {
                if (m_Handle.IsValid()) m_Handle.Stop();

                for (int i = 0; i < m_Instances.Count; i++)
                {
                    m_Instances[i].RemoveComponent<SpawnedEntityComponent>();
                }
            }
        }

        internal struct SpawnedEntityComponent : IEntityComponent
        {
            public Hash m_Hash;
        }
        private sealed class SpawnedEntityComponentProcessor : ComponentProcessor<SpawnedEntityComponent>
        {
            internal static SpawnMapDataModule s_Module;

            protected override void OnDestroy(in InstanceID entity, ref SpawnedEntityComponent component)
            {
                var entry = s_Module.GetEntry(component.m_Hash);
#if DEBUG_MODE
                if (entry == null) return;
#endif
                entry.Remove(entity);
            }
        }
    }

    

}
