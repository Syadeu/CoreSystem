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

using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Entities;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// 유니티 <see cref="Transform"/>을 랩핑하는 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 참조: <seealso cref="UnityTransform"/>
    /// </remarks>
    public interface IUnityTransform : ITransform, IDisposable
    {
#pragma warning disable IDE1006 // Naming Styles
        ConvertedEntity entity { get; }
        Transform provider { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
