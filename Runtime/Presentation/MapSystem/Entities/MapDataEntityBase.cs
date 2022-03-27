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
using Syadeu.Presentation.Data;
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
    [InternalLowLevelEntity]
    public abstract class MapDataEntityBase : EntityDataBase
    {
        [Serializable]
        public abstract class Entry : ICloneable
        {
            public virtual object Clone() => MemberwiseClone();
        }
        [Serializable]
        public sealed class EntityObject : Entry
        {
            [JsonProperty(Order = 0, PropertyName = "Object")] public Reference<EntityBase> m_Object;
            [JsonProperty(Order = 1, PropertyName = "Translation")] public float3 m_Translation;
            [JsonProperty(Order = 2, PropertyName = "Rotation")] public quaternion m_Rotation = quaternion.identity;
            [JsonProperty(Order = 3, PropertyName = "Scale")] public float3 m_Scale;

            [Space]
            [JsonProperty(Order = 4, PropertyName = "Static")]
            public bool m_Static = false;

            [JsonIgnore]
            public AABB aabb
            {
                get
                {
                    EntityBase entity = m_Object.GetObject();
                    return new AABB(entity.Center + m_Translation, entity.Size).Rotation(in m_Rotation, in m_Scale);
                }
            }
            [JsonIgnore] public TRS trs => new TRS(m_Translation, m_Rotation, m_Scale);

            public EntityObject()
            {
                m_Rotation = quaternion.identity;
            }
        }
        [Serializable]
        public sealed class RawObject : Entry
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
        }
        public sealed class EntityDataObject : Entry
        {
            [JsonProperty(Order = 0, PropertyName = "")]
            public Reference<EntityDataBase> m_Object = Reference<EntityDataBase>.Empty;
        }
        public sealed class DataObject : Entry
        {
            [JsonProperty(Order = 0, PropertyName = "")]
            public Reference<DataObjectBase> m_Object = Reference<DataObjectBase>.Empty;
        }

        /// <summary>
        /// 이 데이터에서 프리로드 되야될 에셋을 지정합니다.
        /// </summary>
        /// <returns></returns>
        internal ICustomYieldAwaiter InternalLoadAllAssets() => LoadAllAssets();
        /// <inheritdoc cref="InternalLoadAllAssets"/>
        protected virtual ICustomYieldAwaiter LoadAllAssets() { return null; }
    }
}
