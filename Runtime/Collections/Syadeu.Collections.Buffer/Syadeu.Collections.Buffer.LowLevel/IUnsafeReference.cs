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

namespace Syadeu.Collections.Buffer.LowLevel
{
    public interface IUnsafeReference
    {
        /// <summary>
        /// <paramref name="offset"/> 만큼 
        /// 포인터를 오른쪽으로 밀어서 반환합니다.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        IntPtr this[int offset] { get; }

        /// <summary>
        /// 포인터가 할당되었는지 반환합니다.
        /// </summary>
        bool IsCreated { get; }
        /// <summary>
        /// 포인터입니다.
        /// </summary>
        IntPtr IntPtr { get; }
    }
}
