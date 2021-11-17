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

using Newtonsoft.Json.Utilities;
using Syadeu.Collections;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    [DisplayName("Entity: Object Entity")]
    public sealed class ObjectEntity : EntityBase
    {
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
            AotHelper.EnsureType<EntityData<ObjectEntity>>();
            AotHelper.EnsureList<EntityData<ObjectEntity>>();
            AotHelper.EnsureType<ObjectEntity>();
            AotHelper.EnsureList<ObjectEntity>();
        }
    }
}
