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

using Syadeu.Collections;
using System;

namespace Syadeu.Presentation.Internal
{
    /// <summary>
    /// Presentation Manager에 유저 시스템을 등록하기 위한 인터페이스입니다.<br/>
    /// <see cref="PresentationGroupEntity.RegisterSystem(System.Type[])"/> 을 <see cref="Register"/>에서 호출하여 등록하세요.
    /// </summary>
    /// <remarks>
    /// 클래스에 직접 참조하여 사용하게끔 만들지 않았습니다.<br/>
    /// <seealso cref="PresentationGroupEntity"/>을 참조하여 사용하세요.
    /// </remarks>
    internal interface IPresentationRegister
    {
        bool StartOnInitialize { get; }
        /// <summary>
        /// <see langword="null"/> 이 아닐 경우, 해당 씬이 로드되거나 언로드 되면, 자동으로 활성화되고 비활성화 됩니다.
        /// </summary>
        SceneReference DependenceScene { get; }
        /// <summary>
        /// <see langword="null"/> 이 아닐 경우, 
        /// 해당 그룹(<see cref="PresentationGroupEntity"/>)이 시작될때 같이 시작합니다.
        /// </summary>
        Type DependenceGroup { get; }
        /// <summary>
        /// <see cref="PresentationManager"/>에서 호출되는 메소드입니다.<br/>
        /// <see cref="PresentationGroupEntity.RegisterSystem(System.Type[])"/> 을 여기서 호출하여 등록하세요.
        /// </summary>
        void Register();
    }
}
