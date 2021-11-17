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

using Syadeu.Presentation.Proxy;
using System;

namespace Syadeu.Presentation.Actor
{
    [Flags]
    public enum UpdateType
    {
        Manual = 0,

        Lerp = 0b0001,
        Instant = 0b0010,

        /// <summary>
        /// 카메라와 오리엔테이션을 맞춥니다.
        /// </summary>
        SyncCameraOrientation = 0b0100,
        /// <summary>
        /// 부모(이 엔티티의 <seealso cref="ITransform"/>)와 오리엔테이션을 맞춥니다.
        /// </summary>
        SyncParentOrientation = 0b1000
    }
}
