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
using System;

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="EntityBase"/>의 구성부입니다.<br/><br/>
    /// class 맴버 선언을 그리 추천하고 싶지 않지만, 필요에 의해 선언이 내부에 되었다면,<br/>
    /// 해당 값을 복사하여 인스턴스를 만들기 위해 <see cref="ObjectBase.Copy"/>을 override 하여 해당 값을 복사하여야합니다.<br/>
    /// </summary>
    /// <remarks>
    /// 이 클래스를 상속받음으로서 새로운 어트리뷰트를 선언할 수 있습니다.<br/>
    /// 선언된 클래스는 <seealso cref="EntityDataList"/>에 자동으로 타입이 등록되어 추가할 수 있게 됩니다.
    /// <br/><br/>
    /// <seealso cref="AttributeProcessor"/>를 통해 이 구성부를 이용한 동작부를 선언할 수 있습니다.<br/>
    /// <see cref="AttributeBase"/>와 <seealso cref="AttributeProcessor"/>는 하나처럼 작동합니다.
    /// </remarks>
    public interface IAttribute : IObject
    {
        /// <summary>
        /// 이 어트리뷰트의 이름입니다.
        /// </summary>
        [JsonProperty(Order = -11)] string Name { get; }
        /// <summary>
        /// 이 어트리뷰트의 고유 해쉬 값입니다. <seealso cref="EntityDataList"/>
        /// </summary>
        [JsonProperty(Order = -10)] Hash Hash { get; }
        
        /// <summary>
        /// 이 어트리뷰트의 부모 <see cref="IEntityData"/>입니다.
        /// </summary>
        /// <remarks>
        /// 런타임 중에만 부모가 할당되어 동작합니다.
        /// </remarks>
        [JsonIgnore] IEntityData ParentEntity { get; }
    }
}
