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

namespace Syadeu
{
    public enum CoreSystemExceptionFlag
    {
        /// <summary>
        /// 에디터에서 발생한 예외사항입니다.
        /// </summary>
        Editor,

        /// <summary>
        /// 잡을 실행하는 도중 발생한 예외사항입니다.
        /// </summary>
        Jobs,
        /// <summary>
        /// ECS에서 발생한 예외사항입니다.
        /// </summary>
        ECS,

        /// <summary>
        /// 백그라운드 스레드에서 발생한 예외사항입니다.
        /// </summary>
        Background,
        /// <summary>
        /// 메인 유니티 스레드에서 발생한 예외사항입니다.
        /// </summary>
        Foreground,

        /// <summary>
        /// 재사용 오브젝트에서 발생한 예외사항입니다.
        /// </summary>
        RecycleObject,
        /// <summary>
        /// 랜더 매니저에서 발생한 예외사항입니다.
        /// </summary>
        Render,
        /// <summary>
        /// 콘솔에서 발생한 예외사항입니다.
        /// </summary>
        Console,
        /// <summary>
        /// 데이터관련 메소드에서 예외사항입니다.
        /// </summary>
        Database,
        /// <summary>
        /// 모노 기반 오브젝트에서 발생한 예외사항입니다.
        /// </summary>
        Mono,

        Presentation,
        Proxy,
    }
}
