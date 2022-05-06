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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;

namespace Syadeu.Presentation
{
    /// <summary>
    /// Application initialize 단계에서 에셋을 프리로드할 수 있는 interface 입니다.
    /// </summary>
    public interface IPrefabPreloader
    {
        /// <summary>
        /// Preload 를 등록하는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="PrefabPreloader.Add(PrefabReference)"/> 로 등록할 수 있습니다.
        /// </remarks>
        /// <param name="loader"></param>
        void Register(PrefabPreloader loader);
    }

}
