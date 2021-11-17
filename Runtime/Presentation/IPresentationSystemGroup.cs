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

using Syadeu.Collections;
using Syadeu.Presentation.Internal;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 이 구현부의 직접 상속은 허용하지 않습니다.<br/>
    /// <see cref="PresentationSystemGroup{T}"/> 을 사용하세요
    /// </summary>
    public interface IPresentationSystemGroup : IValidation, System.IEquatable<IPresentationSystemGroup>
    {
        Hash GroupHash { get; }

        /// <summary>
        /// 이 그룹내에서 실행되고 있는 <see cref="PresentationSystem{T}"/>들의 리스트를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 런타임 중 시스템 추가는 허용하지 않습니다.<br/>
        /// <seealso cref="PresentationGroupEntity"/>를 상속받는 레지스터 클래스를 선언 후 등록하세요.
        /// </remarks>
        IReadOnlyList<PresentationSystemEntity> Systems { get; }

        /// <summary>
        /// 이 그룹내 모든 시스템(<seealso cref="PresentationSystem{T}"/>)을 실행합니다.
        /// </summary>
        ICustomYieldAwaiter Start();
        /// <summary>
        /// 이 그룹내 모든 시스템(<seealso cref="PresentationSystem{T}"/>)을 정지합니다.
        /// </summary>
        void Stop();
    }
}
