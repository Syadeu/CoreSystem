// Copyright 2022 Seung Ha Kim
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Syadeu.Collections;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Syadeu.Presentation.Map
{
    [DisplayName("MapData: Const Map Data Entity")]
    [EntityAcceptOnly(typeof(MapDataAttributeBase))]
    [Description(
        "데이터만 생성하는 오브젝트 데이터 테이블 입니다.")]
    public sealed class ConstMapDataEntity : MapDataEntityBase
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "DataObjects")]
        public ArrayWrapper<Reference<DataObjectBase>> m_DataObjects = Array.Empty<Reference<DataObjectBase>>();

        [JsonIgnore]
        internal Entity<DataObjectBase>[] m_CreatedEntities = Array.Empty<Entity<DataObjectBase>>();
    }
    internal sealed class ConstMapDataEntityProcessor : EntityProcessor<ConstMapDataEntity>
    {
        protected override void OnCreated(ConstMapDataEntity obj)
        {
            obj.m_CreatedEntities = new Entity<DataObjectBase>[obj.m_DataObjects.Length];
            for (int i = 0; i < obj.m_DataObjects.Length; i++)
            {
                obj.m_CreatedEntities[i] = obj.m_DataObjects[i].CreateEntity();
            }
        }
        protected override void OnDestroy(ConstMapDataEntity obj)
        {
            for (int i = 0; i < obj.m_CreatedEntities.Length; i++)
            {
                obj.m_CreatedEntities[i].Destroy();
            }
            obj.m_CreatedEntities = Array.Empty<Entity<DataObjectBase>>();
        }
    }
}
