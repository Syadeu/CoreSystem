﻿// Copyright 2021 Seung Ha Kim
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

using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Mono;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [DisplayName("EntityData: SceneData")]
    [EntityAcceptOnly(typeof(SceneDataAttributeBase))]
    public sealed class SceneDataEntity : EntityDataBase, INotifySceneAsset,
        INotifyComponent<SceneDataComponent>
    {
        [JsonProperty(Order = 0, PropertyName = "TerrainData")]
        internal Reference<TerrainData>[] m_TerrainData = Array.Empty<Reference<TerrainData>>();
        
        [Space]
#pragma warning disable IDE0044 // Add readonly modifier
        [Tooltip("SceneIndex 의 씬이 로드될때 자동으로 데이터를 생성하나요?")]
        [JsonProperty(Order = 1, PropertyName = "BindScene")] internal bool m_BindScene;
        [Tooltip("SceneList.Scenes 의 Index")]
        [JsonProperty(Order = 2, PropertyName = "SceneIndex")] private int m_SceneIndex;
        [JsonProperty(Order = 3, PropertyName = "MapData")] private Reference<MapDataEntity>[] m_MapData = Array.Empty<Reference<MapDataEntity>>();
#pragma warning restore IDE0044 // Add readonly modifier

        [JsonIgnore] internal InstanceArray<TerrainData> m_CreatedTerrains;

        [JsonIgnore] public IReadOnlyList<Reference<MapDataEntity>> MapData => m_MapData;
        [JsonIgnore] public EntityData<MapDataEntity>[] CreatedMapData { get; private set; }

        [JsonIgnore] public bool DestroyChildOnDestroy { get; set; } = true;

        SceneReference INotifySceneAsset.TargetScene => GetTargetScene();
        IEnumerable<IPrefabReference> INotifyAsset.NotifyAssets
        {
            get
            {
                var iters = m_TerrainData.Select(other => ((INotifyAsset)other.GetObject()).NotifyAssets);
                List<IPrefabReference> list = new List<IPrefabReference>();
                foreach (var item in iters)
                {
                    list.AddRange(item);
                }

                return list;
            }
        }

        public override bool IsValid()
        {
            if (m_BindScene)
            {
                if (m_SceneIndex < 0 || SceneList.Instance.Scenes.Count <= m_SceneIndex) return false;
            }
            if (m_MapData == null || m_MapData.Length == 0) return false;
            return true;
        }

        public SceneReference GetTargetScene()
        {
            if (m_SceneIndex < 0 || SceneList.Instance.Scenes.Count <= m_SceneIndex) return null;
            return SceneList.Instance.Scenes[m_SceneIndex];
        }
        public ICustomYieldAwaiter LoadAllAssets()
        {
            List<ICustomYieldAwaiter> awaiters = new List<ICustomYieldAwaiter>();
            for (int i = 0; i < m_MapData.Length; i++)
            {
                awaiters.Add(m_MapData[i].GetObject().LoadAllAssets());
            }

            return new Awaiter(awaiters);
        }

        private sealed class Awaiter : ICustomYieldAwaiter
        {
            private readonly IEnumerable<ICustomYieldAwaiter> m_Awaiters;

            public Awaiter(IEnumerable<ICustomYieldAwaiter> awaiters)
            {
                m_Awaiters = awaiters;
            }

            public bool KeepWait
            {
                get
                {
                    foreach (var item in m_Awaiters)
                    {
                        if (item.KeepWait)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<SceneDataEntity>>();
            AotHelper.EnsureList<Reference<SceneDataEntity>>();
            AotHelper.EnsureType<EntityData<SceneDataEntity>>();
            AotHelper.EnsureList<EntityData<SceneDataEntity>>();
            AotHelper.EnsureType<SceneDataEntity>();
            AotHelper.EnsureList<SceneDataEntity>();
        }
    }
    [Preserve]
    internal sealed class SceneDataEntityProcessor : EntityDataProcessor<SceneDataEntity>
    {
        protected override void OnCreated(SceneDataEntity entity)
        {
            //if (!entity.Target.IsValid()) return;

            //entity.Target.CreateMapData(EntitySystem);
            //SceneDataEntity sceneData = entity.Target;

            //sceneData.m_CreatedTerrains = new InstanceArray<TerrainData>(sceneData.m_TerrainData, Allocator.Persistent);
            //for (int i = 0; i < sceneData.m_CreatedTerrains.Length; i++)
            //{
            //    sceneData.m_CreatedTerrains[i].Object.Create(null);
            //}

            CreateMapData(entity);
        }
        private void CreateMapData(SceneDataEntity sceneDataEntity)
        {
            IReadOnlyList<Reference<MapDataEntity>> mapData = sceneDataEntity.MapData;

            EntityData<IEntityData> entity = EntityData<IEntityData>.GetEntityWithoutCheck(sceneDataEntity.Idx);
            entity.AddComponent<SceneDataComponent>();

            ref SceneDataComponent sceneData = ref entity.GetComponent<SceneDataComponent>();
            sceneData.m_Created = true;

            //sceneData.m_CreatedMapData = new InstanceArray<MapDataEntity>(mapData.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            sceneData.m_CreatedMapData = new FixedInstanceList64<MapDataEntity>();
            for (int i = 0; i < mapData.Count; i++)
            {
                if (mapData[i].IsEmpty() || !mapData[i].IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"MapData(Element At {i}) in SceneData({sceneDataEntity.Name}) is not valid.");

                    //sceneData.m_CreatedMapData.Add(Instance<MapDataEntity>.Empty);
                    continue;
                }

                EntityData<IEntityData> temp = EntitySystem.CreateObject(mapData[i]);
                sceneData.m_CreatedMapData.Add(new Instance<MapDataEntity>(temp));
            }

            //sceneData.m_CreatedTerrains = new InstanceArray<TerrainData>(sceneDataEntity.m_TerrainData, Allocator.Persistent);
            sceneData.m_CreatedTerrains = new FixedInstanceList64<TerrainData>();
            for (int i = 0; i < sceneDataEntity.m_TerrainData.Length; i++)
            {
                if (sceneDataEntity.m_TerrainData[i].IsEmpty() ||
                    !sceneDataEntity.m_TerrainData[i].IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"TerrainData(Element At {i}) in SceneData({sceneDataEntity.Name}) is not valid.");

                    continue;
                }

                Instance<TerrainData> temp = EntitySystem.CreateInstance(sceneDataEntity.m_TerrainData[i]);
                sceneData.m_CreatedTerrains.Add(temp);
            }

            for (int i = 0; i < sceneData.m_CreatedTerrains.Length; i++)
            {
                sceneData.m_CreatedTerrains[i].GetObject().Create(null);
            }
        }
        protected override void OnDestroy(SceneDataEntity entity)
        {
            //if (entity.Target == null || !entity.Target.IsValid()) return;

            //entity.Target.DestroyMapData();
            //SceneDataEntity sceneData = entity.Target;
            //for (int i = 0; i < sceneData.m_CreatedTerrains.Length; i++)
            //{
            //    sceneData.m_CreatedTerrains[i].Destroy();
            //}
            //sceneData.m_CreatedTerrains.Dispose();
        }
    }
}
