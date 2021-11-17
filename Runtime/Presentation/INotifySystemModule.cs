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


namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="PresentationSystemEntity{T}"/> 에 서브 모듈(<typeparamref name="TModule"/>)을 추가합니다.
    /// </summary>
    /// <remarks>
    /// 서브 모듈은 항상 메인 시스템이 동작한 이후에 수행됩니다.
    /// </remarks>
    /// <typeparam name="TModule"></typeparam>
    public interface INotifySystemModule<TModule> where TModule : PresentationSystemModule
    {
    }
}
