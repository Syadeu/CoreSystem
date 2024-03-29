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

using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Components
{
    /// <summary>
    /// 이 <see cref="ObjectBase"/> 에 종속된 <see cref="IEntityComponent"/> 를 선언합니다.
    /// 선언된 컴포넌트는 해당 오브젝트 파괴시 자동으로 제거됩니다.
    /// <seealso cref="System.IDisposable"/>을 컴포넌트가 상속받고 있다면 자동으로 수행합니다.
    /// </summary>
    /// <remarks>
    /// 사용자가 유동적으로 컴포넌트를 추가 및 제거하고 싶다면 이 구현부를 상속받아서는 안됩니다.<br/>
    /// 이 구현부로 선언된 컴포넌트(<typeparamref name="TComponent"/>)는 사용자에 의해 제거되서는 안됩니다.
    /// </remarks>
    /// <typeparam name="TComponent"></typeparam>
    public interface INotifyComponent<TComponent> where TComponent : unmanaged, IEntityComponent
    {
    }
}
