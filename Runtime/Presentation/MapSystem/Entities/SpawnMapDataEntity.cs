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

using Newtonsoft.Json;
using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Map
{
    [DisplayName("MapData: Spawn Map Data Entity")]
    [EntityAcceptOnly(typeof(MapDataAttributeBase))]
    [Description(
        "스폰 테이블")]
    public sealed class SpawnMapDataEntity : MapDataEntityBase
    {
        [Serializable]
        public sealed class Point : Entry
        {
            [UnityEngine.SerializeField]
            [JsonProperty(Order = -1, PropertyName = "Name")]
            public string m_Name = "NewPoint";

            [Space]
            [UnityEngine.SerializeField]
            [JsonProperty(Order = 0, PropertyName = "TargetEntity")]
            public Reference<EntityBase> m_TargetEntity = Reference<EntityBase>.Empty;
            [UnityEngine.SerializeField]
            [PositionHandle]
            [JsonProperty(Order = 1, PropertyName = "Position")]
            public float3 m_Position = 0;
            [ScaleHandle("m_Position")]
            [UnityEngine.SerializeField]
            [JsonProperty(Order = 2, PropertyName = "SphereOffset")]
            public float3 m_SphereOffset = 0;

            [Space]
            [UnityEngine.SerializeField]
            [JsonProperty(Order = 3, PropertyName = "EnableAutoSpawn")]
            public bool m_EnableAutoSpawn = false;
            [UnityEngine.SerializeField]
            [JsonProperty(Order = 4, PropertyName = "PerTime")]
            [Description(
                "EnableAutoSpawn = true 일 경우에만 " +
                "x = hour, y = mins, z = secs")]
            public int3 m_PerTime = new int3(0, 0, 30);
            [UnityEngine.SerializeField]
            [JsonProperty(Order = 5, PropertyName = "TimeOffset")]
            public int3 m_TimeOffset = 0;

            [Space]
            [UnityEngine.SerializeField]
            [JsonProperty(Order = 6, PropertyName = "SpawnAtStart")]
            public bool m_SpawnAtStart = true;
            [UnityEngine.SerializeField]
            [JsonProperty(Order = 7, PropertyName = "MaximumCount")]
            public int m_MaximumCount = 1;

            public Hash GetHash() => Hash.NewHash(m_Name);
        }

        [JsonProperty(Order = 0, PropertyName = "Points")]
        public Point[] m_Points = Array.Empty<Point>();
    }
    internal sealed class SpawnMapDataEntityProcessor : EntityProcessor<SpawnMapDataEntity>
    {
        private MapSystem m_MapSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, MapSystem>(Bind);
        }
        private void Bind(MapSystem other)
        {
            m_MapSystem = other;
        }
        protected override void OnDispose()
        {
            m_MapSystem = null;
        }

        protected override void OnCreated(SpawnMapDataEntity obj)
        {
            m_MapSystem.AddSpawnEntity(obj.m_Points);
        }
        protected override void OnDestroy(SpawnMapDataEntity obj)
        {
            m_MapSystem.RemoveSpawnEntity(obj.m_Points);
        }
    }
}
