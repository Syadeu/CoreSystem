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

using System;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// <see cref="RecycleableMonobehaviour"/> 에서 유니티 컴포넌트에게 고유 ID를 부여하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// <seealso cref="RecycleableMonobehaviour.m_Components"/>
    /// </remarks>
    public interface IComponentID : IEquatable<IComponentID>
    {
        ulong Hash { get; }
    }
}
