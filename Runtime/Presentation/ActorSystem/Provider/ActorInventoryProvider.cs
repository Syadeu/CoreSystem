﻿// Copyright 2022 Seung Ha Kim
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
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;
using Unity.Collections;
using Newtonsoft.Json;
using System.ComponentModel;
using Unity.Mathematics;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("ActorProvider: Inventory Provider")]
    public sealed class ActorInventoryProvider : ActorProviderBase<ActorInventoryComponent> 
        //IPrefabPreloader
    {
        [Serializable]
        public sealed class GraphicsInformation : PropertyBlock<GraphicsInformation>
        {
            [SerializeField, JsonProperty(Order = 0, PropertyName = "UIPrefab")]
            public Reference<UIObjectEntity> m_UIPrefab = Reference<UIObjectEntity>.Empty;

            [Space]
            [SerializeField, JsonProperty(Order = 1, PropertyName = "PositionOffset")]
            public float3 m_PositionOffset;
            [SerializeField, JsonProperty(Order = 2, PropertyName = "RotationOffset")]
            public float3 m_RotationOffset;
        }

        [SerializeField, JsonProperty(Order = 0, PropertyName = "Space")]
        public LinkedBlock m_Space = new LinkedBlock();

        [SerializeField, JsonProperty(Order = 1, PropertyName = "GraphicsInfo")]
        public GraphicsInformation m_GraphicsInfo = new GraphicsInformation();

        //[SerializeField]
        //public PrefabReference<ActorInventoryMonobehaviour> m_InventoryPrefab = PrefabReference<ActorInventoryMonobehaviour>.None;

        protected override void OnInitialize(in Entity<IEntityData> parent, ref ActorInventoryComponent component)
        {
            component = new ActorInventoryComponent(parent.Idx, m_Space);

            //m_InventoryPrefab.GetOrCreateInstance
        }

        //void IPrefabPreloader.Register(PrefabPreloader loader)
        //{
        //    if (m_InventoryPrefab.IsNone() || !m_InventoryPrefab.IsValid()) return;

        //    loader.Add(m_InventoryPrefab);
        //}
    }

    public struct ActorInventoryComponent : IActorProviderComponent, IDisposable
    {
        private ItemInventory m_Inventory;

        public ItemInventory Inventory => m_Inventory;

        public ActorInventoryComponent(InstanceID owner, LinkedBlock block)
        {
            m_Inventory = new ItemInventory(owner, block, Allocator.Persistent);
        }

        void IDisposable.Dispose()
        {
            m_Inventory.Dispose();
        }
    }
}
