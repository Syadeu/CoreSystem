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

namespace Syadeu
{
    /// <summary>
    /// <see cref="CoreSystem"/> framework 내 코루틴에서 사용할 수 있는 awaiter 입니다.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> 반환은 <seealso cref="NullCustomYieldAwaiter"/> 를 참조하세요.
    /// </remarks>
    public interface ICustomYieldAwaiter
    {
        bool KeepWait { get; }
    }
}
