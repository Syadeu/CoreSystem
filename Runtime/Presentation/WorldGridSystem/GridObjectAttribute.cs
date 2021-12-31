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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System.ComponentModel;

namespace Syadeu.Presentation.Grid.LowLevel
{
    [AttributeAcceptOnly(typeof(EntityBase))]
    [DisplayName("Attribute: Grid Object")]
    public sealed class GridObjectAttribute : AttributeBase
    {

    }
    internal sealed class GridObjectAttributeProcessor : AttributeProcessor<GridObjectAttribute>
    {
        private WorldGridSystem m_GridSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, WorldGridSystem>(Bind);
        }
        private void Bind(WorldGridSystem other)
        {
            m_GridSystem = other;
        }
        protected override void OnDispose()
        {
            m_GridSystem = null;
        }

        protected override void OnCreated(GridObjectAttribute attribute, EntityData<IEntityData> entity)
        {
            m_GridSystem.AddEntity(entity.Idx);
        }
        protected override void OnDestroy(GridObjectAttribute attribute, EntityData<IEntityData> entity)
        {
            m_GridSystem.RemoveEntity(entity.Idx);
        }
    }
}