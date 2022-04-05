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

using Newtonsoft.Json;
using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.ThreadSafe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Map
{
    [DisplayName("MapData: Map Data Entity")]
    [EntityAcceptOnly(typeof(MapDataAttributeBase))]
    [Description("파괴되지않는 오브젝트들로 구성된 오브젝트 데이터 테이블 입니다.")]
    public sealed class MapDataEntity : MapDataEntityBase
    {
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 0, PropertyName = "Center")] public float3 m_Center = float3.zero;
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 1, PropertyName = "Objects")] public ArrayWrapper<EntityObject> m_Objects = Array.Empty<EntityObject>();
        [UnityEngine.SerializeField]
        [JsonProperty(Order = 2, PropertyName = "RawObjects")] public ArrayWrapper<RawObject> m_RawObjects = Array.Empty<RawObject>();

        protected override ICustomYieldAwaiter LoadAllAssets()
        {
            return new Awaiter(m_Objects, m_RawObjects);
        }

        private sealed class Awaiter : ICustomYieldAwaiter
        {
            private readonly int m_AssetCount;
            private int m_Counter;

            public Awaiter(EntityObject[] objs, RawObject[] rawObjs)
            {
                AsyncOperationHandle<GameObject> handle;
                m_Counter = 0;

                IEnumerable<PrefabReference<GameObject>> temp1 = objs
                    .Select(other => other.m_Object.GetObject().Prefab);
                foreach (var item in temp1)
                {
                    if (item.IsNone())
                    {
                        Interlocked.Increment(ref m_Counter);

                        continue;
                    }

                    if (!item.IsValid())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"MapDataEntity() trying to load an invalid entity.");

                        Interlocked.Increment(ref m_Counter);

                        continue;
                    }

                    if (item.Asset == null)
                    {
                        handle = item.LoadAssetAsync();
                        handle.CompletedTypeless += Handle_CompletedTypeless;
                    }
                    else Interlocked.Increment(ref m_Counter);
                }
                m_AssetCount += temp1.Count();

                IEnumerable<PrefabReference<GameObject>> temp2 = rawObjs
                    .Select(other => other.m_Object);
                foreach (var item in temp2)
                {
                    if (item.IsNone())
                    {
                        Interlocked.Increment(ref m_Counter);

                        continue;
                    }

                    if (!item.IsValid())
                    {
                        CoreSystem.Logger.LogError(Channel.Entity,
                            $"MapDataEntity() trying to load an invalid entity.");

                        Interlocked.Increment(ref m_Counter);

                        continue;
                    }

                    handle = item.LoadAssetAsync();
                    handle.CompletedTypeless += Handle_CompletedTypeless;
                }

                m_AssetCount += temp2.Count();
            }

            private void Handle_CompletedTypeless(AsyncOperationHandle obj)
            {
                Interlocked.Increment(ref m_Counter);
            }

            public bool KeepWait => m_Counter != m_AssetCount;
        }

        [JsonIgnore] public Entity<EntityBase>[] CreatedEntities { get; internal set; }
        [JsonIgnore] public ProxyTransform[] CreatedRawObjects { get; internal set; }
        //[JsonIgnore] public bool DestroyChildOnDestroy { get; set; } = true;

        public override bool IsValid() => true;
        protected override ObjectBase Copy()
        {
            MapDataEntity clone = (MapDataEntity)base.Copy();
            EntityObject[] temp = new EntityObject[m_Objects.Length];
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = (EntityObject)m_Objects[i].Clone();
            }
            clone.m_Objects = temp;
            clone.CreatedEntities = null;
            //clone.DestroyChildOnDestroy = true;

            return clone;
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<MapDataEntity>>();
            AotHelper.EnsureList<Reference<MapDataEntity>>();
            AotHelper.EnsureType<Entity<MapDataEntity>>();
            AotHelper.EnsureList<Entity<MapDataEntity>>();
            AotHelper.EnsureType<MapDataEntity>();
            AotHelper.EnsureList<MapDataEntity>();
        }
    }
    public sealed class MapDataProcessor : EntityProcessor<MapDataEntity>
    {
        protected override void OnCreated(MapDataEntity entity)
        {
            entity.CreatedEntities = new Entity<EntityBase>[entity.m_Objects.Length];
            for (int i = 0; i < entity.m_Objects.Length; i++)
            {
                if (entity.m_Objects[i].m_Object.IsEmpty() || 
                    !entity.m_Objects[i].m_Object.IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot spawn map object in [{entity.Name}] element at {i} is not valid.");

                    entity.CreatedEntities[i] = Entity<EntityBase>.Empty;
                    continue;
                }

                entity.CreatedEntities[i] = CreateEntity(entity.m_Objects[i].m_Object, entity.m_Objects[i].m_Translation, entity.m_Objects[i].m_Rotation, entity.m_Objects[i].m_Scale);
            }

            entity.CreatedRawObjects = new ProxyTransform[entity.m_RawObjects.Length];
            for (int i = 0; i < entity.m_RawObjects.Length; i++)
            {
                if (entity.m_RawObjects[i].m_Object.IsNone() ||
                    !entity.m_RawObjects[i].m_Object.IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Cannot spawn map object in [{entity.Name}] element at {i} raw object is not valid.");

                    continue;
                }

                entity.CreatedRawObjects[i] = ProxySystem.CreateNewPrefab(entity.m_RawObjects[i].m_Object, entity.m_RawObjects[i].m_Translation, entity.m_RawObjects[i].m_Rotation, entity.m_RawObjects[i].m_Scale, true,
                    entity.m_RawObjects[i].m_Center, entity.m_RawObjects[i].m_Size, true);
            }
        }
        protected override void OnDestroy(MapDataEntity entity)
        {
            //if (entity == null || !entity.DestroyChildOnDestroy) return;
            for (int i = 0; i < entity.CreatedEntities.Length; i++)
            {
                if (entity.CreatedEntities[i].IsValid())
                {
                    EntitySystem.DestroyEntity(entity.CreatedEntities[i]);
                }
            }
            entity.CreatedEntities = null;

            for (int i = 0; i < entity.CreatedRawObjects.Length; i++)
            {
                if (!entity.CreatedRawObjects[i].isDestroyed)
                {
                    entity.CreatedRawObjects[i].Destroy();
                }
            }
        }
    }
}
