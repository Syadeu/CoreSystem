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
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using AABB = Syadeu.Collections.AABB;

namespace Syadeu.Presentation.Map
{
    public abstract class MapDataEntityBase : EntityDataBase
    {
        [Serializable]
        public sealed class Object : ICloneable
        {
            [JsonProperty(Order = 0, PropertyName = "Object")] public Reference<EntityBase> m_Object;
            [JsonProperty(Order = 1, PropertyName = "Translation")] public float3 m_Translation;
            [JsonProperty(Order = 2, PropertyName = "Rotation")] public quaternion m_Rotation = quaternion.identity;
            [JsonProperty(Order = 3, PropertyName = "Scale")] public float3 m_Scale;

            [Space]
            [JsonProperty(Order = 4, PropertyName = "Static")]
            public bool m_Static = false;

            [JsonIgnore] public AABB aabb
            {
                get
                {
                    EntityBase entity = m_Object.GetObject();
                    return new AABB(entity.Center + m_Translation, entity.Size).Rotation(in m_Rotation, in m_Scale);
                }
            }
            [JsonIgnore] public TRS trs => new TRS(m_Translation, m_Rotation, m_Scale);

            public Object()
            {
                m_Rotation = quaternion.identity;
            }
            public object Clone() => MemberwiseClone();
        }
        [Serializable]
        public sealed class RawObject : ICloneable
        {
            [JsonProperty(Order = 0, PropertyName = "Object")] public PrefabReference<GameObject> m_Object;
            [JsonProperty(Order = 1, PropertyName = "Translation")] public float3 m_Translation;
            [JsonProperty(Order = 2, PropertyName = "Rotation")] public quaternion m_Rotation = quaternion.identity;
            [JsonProperty(Order = 3, PropertyName = "Scale")] public float3 m_Scale;

            [Space]
            [JsonProperty(Order = 4, PropertyName = "Center")]
            public float3 m_Center;
            [JsonProperty(Order = 5, PropertyName = "Size")]
            public float3 m_Size = 1;

            [Space]
            [JsonProperty(Order = 6, PropertyName = "Static")]
            public bool m_Static = false;

            [JsonIgnore]
            public AABB aabb
            {
                get
                {
                    return new AABB(m_Center + m_Translation, m_Size).Rotation(in m_Rotation, in m_Scale);
                }
            }
            [JsonIgnore] public TRS trs => new TRS(m_Translation, m_Rotation, m_Scale);

            public RawObject()
            {
                m_Rotation = quaternion.identity;
            }
            public object Clone() => MemberwiseClone();
        }

        [JsonProperty(Order = 0, PropertyName = "Center")] public float3 m_Center = float3.zero;
        [JsonProperty(Order = 1, PropertyName = "Objects")] public Object[] m_Objects = Array.Empty<Object>();
        [JsonProperty(Order = 2, PropertyName = "RawObjects")] public RawObject[] m_RawObjects = Array.Empty<RawObject>();

        public ICustomYieldAwaiter LoadAllAssets()
        {
            return new Awaiter(m_Objects, m_RawObjects);
        }

        private sealed class Awaiter : ICustomYieldAwaiter
        {
            private readonly int m_AssetCount;
            private int m_Counter;

            public Awaiter(Object[] objs, RawObject[] rawObjs)
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
    }
}
