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

namespace Syadeu.Mono.Console
{
    [System.Flags]
    public enum CommandSetting
    {
        None = 0,

        /// <summary>
        /// Requires 프로퍼티의 리턴이 예상값과 일치할 경우에만 자동완성 및 콘솔창에 표시됩니다.
        /// </summary>
        ShowIfRequiresTrue = 1 << 0,

        All = ~0
    }
}
