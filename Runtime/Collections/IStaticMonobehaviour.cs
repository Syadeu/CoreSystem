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


namespace Syadeu.Collections
{
    public interface IStaticMonobehaviour
    {
        /// <summary>
        /// 객체 관리에서 발생하는 로그를 출력시킬 것인지 설정합니다.
        /// </summary>
        /// <remarks>
        /// 기본 값은 <see langword="true"/> 입니다.
        /// </remarks>
        bool EnableLog { get; }
        /// <summary>
        /// Hierarchy 에서 숨겨질지 설정합니다.
        /// </summary>
        /// <remarks>
        /// 기본 값은 <see langword="false"/> 입니다.
        /// </remarks>
        bool HideInInspector { get; }

        /// <summary>
        /// 객체가 생성되었을 때 수행하는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// Awake -> OnEnable -> OnInitialize -> Start 순 입니다. <br/>
        /// 이 메소드내에서 다른 <see cref="StaticMonobehaviour{T}"/>, 
        /// 혹은 자기 자신의 인스턴스를 호출하면 stack overflow 가 발생할 수 있습니다. 
        /// 해당 작업은 Awake, 혹은 Start 에서 수행되어야 합니다.
        /// </remarks>
        void OnInitialize();
        /// <summary>
        /// 시스템이 종료될 때 (ex. <see cref="UnityEngine.Application.Quit"/>) 수행하는 메소드입니다.
        /// </summary>
        /// <remarks>
        /// OnShutDown -> OnDisable -> OnDestroy 순 입니다.
        /// </remarks>
        void OnShutdown();
    }
}
