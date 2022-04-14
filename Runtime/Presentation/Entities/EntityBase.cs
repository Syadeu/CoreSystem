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
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Proxy;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Presentation.Entities
{
    /// <summary><inheritdoc cref="IEntity"/></summary>
    /// <remarks>
    /// class 맴버 선언을 그리 추천하고 싶지 않지만, 필요에 의해 선언이 내부에 되었다면,<br/>
    /// 해당 값을 복사하여 인스턴스를 만들기 위해 <see cref="ObjectBase.Copy"/>을 override 하여 해당 값을 복사하여야합니다.<br/>
    /// <br/>
    /// 이 클래스를 상속받음으로서 새로운 오브젝트를 선언할 수 있습니다.<br/>
    /// 선언된 클래스는 <seealso cref="EntityDataList"/>에 자동으로 타입이 등록되어 추가할 수 있게 됩니다.
    /// </remarks>
    [InternalLowLevelEntity]
    public abstract class EntityBase : EntityDataBase, IEntity
    {
        /// <summary>
        /// 이 엔티티의 Raw 프리팹 주소입니다.
        /// </summary>
        [SerializeField, JsonProperty(Order = -800, PropertyName = "Prefab")] 
        public PrefabReference<GameObject> Prefab = PrefabReference<GameObject>.None;

        [SerializeField, JsonProperty(Order = -799, PropertyName = "StaticBatching")]
        public bool StaticBatching;

        [Description("AABB 의 Center")]
        [SerializeField, PositionHandle(ScaleField = "Size")]
        [JsonProperty(Order = -798, PropertyName = "Center")]
        public float3 Center;

        [Description("AABB 의 Size")]
        [SerializeField, ScaleHandle(PositionField = "Center")]
        [JsonProperty(Order = -797, PropertyName = "Size")]
        public float3 Size = 1;

        [Space]
        [SerializeField, JsonProperty(Order = -796, PropertyName = "EnableCull")] 
        private bool m_EnableCull = true;

        [JsonIgnore] public virtual bool EnableCull => m_EnableCull;
        [JsonIgnore] float3 IEntity.Center => Center;
        [JsonIgnore] float3 IEntity.Size => Size;

        public override bool IsValid()
        {
            if (Reserved) return false;

            return true;
        }
        public void SetCulling(bool enable)
        {
            m_EnableCull = enable;
        }

        [Preserve]
        static void AOTCodeGeneration()
        {
            AotHelper.EnsureType<Reference<EntityBase>>();
            AotHelper.EnsureList<Reference<EntityBase>>();

            AotHelper.EnsureType<Entity<EntityBase>>();
            AotHelper.EnsureList<Entity<EntityBase>>();

            AotHelper.EnsureList<EntityBase>();
        }
    }
}
