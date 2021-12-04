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
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Attributes
{
    [DisplayName("Attribute: Create Entity")]
    [Obsolete("이 어트리뷰트는 예제로 작성되었습니다.")]
    [Description(
        "이 어트리뷰트가 달린 Entity가 생성될 때, 같이 생성될 Entity 를 설정할 수 있습니다."
        )]
    public sealed class CreateEntityAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "Entity")] public Reference<EntityBase> m_Entity;
        
        [JsonIgnore] public Entity<EntityBase> CreatedEntity { get; internal set; }
    }
    [Preserve, Obsolete]
    internal sealed class CreateEntityProcessor : AttributeProcessor<CreateEntityAttribute>
    {
        protected override void OnCreated(CreateEntityAttribute attribute, EntityData<IEntityData> entity)
        {
            ITransform tr = entity.As<IEntityData, IEntity>().Target.transform;
            attribute.CreatedEntity = CreateEntity(attribute.m_Entity, tr.position, tr.rotation);
        }
    }
}
