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

using System;

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="StaticScriptableObject{T}"/> 의 데이터 저장 경로를 지정할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 저장 경로는 Assets/Resources/ 부터 시작합니다. 
    /// <seealso cref="Path"/> 는 파일 이름을 제외한 폴더 경로만 받습니다.
    /// </remarks>
    public sealed class AssetPathAttribute : Attribute
    {
        public string Path;
    }
}
