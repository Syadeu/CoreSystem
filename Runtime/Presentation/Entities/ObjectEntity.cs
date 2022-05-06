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
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [DisplayName("Entity: Object Entity")]
    [InternalLowLevelEntity]
    public sealed class ObjectEntity : EntityBase
    {
        [SerializeField, JsonProperty(Order = 1, PropertyName = "Events")]
        internal EntityEventProperty m_Events = new EntityEventProperty();

        protected override ObjectBase Copy()
        {
            ObjectEntity clone = (ObjectEntity)base.Copy();
            return clone;
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<ObjectEntity>>();
            AotHelper.EnsureList<Reference<ObjectEntity>>();
            AotHelper.EnsureType<Entity<ObjectEntity>>();
            AotHelper.EnsureList<Entity<ObjectEntity>>();
            AotHelper.EnsureType<ObjectEntity>();
            AotHelper.EnsureList<ObjectEntity>();
        }
    }
    internal sealed class ObjectEntityProcessor : EntityProcessor<ObjectEntity>
    {
        protected override void OnCreated(ObjectEntity obj)
        {
            obj.m_Events.ExecuteOnCreated(obj.Idx);
        }
        protected override void OnDestroy(ObjectEntity obj)
        {
            obj.m_Events.ExecuteOnDestroy(obj.Idx);
        }
    }
}
