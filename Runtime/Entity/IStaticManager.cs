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

namespace Syadeu.Entities
{
    public interface IStaticManager : System.IDisposable
    {
        bool Disposed { get; }

        /// <summary>
        /// 인스턴트가 생성될때 한번 실행할 함수입니다.
        /// </summary>
        void OnInitialize();
        /// <summary>
        /// 초기화 작업이 완료되고 마지막에 실행되는 함수입니다.
        /// </summary>
        void OnStart();
        /// <summary>
        /// 초기화 함수입니다.
        /// </summary>
        void Initialize(SystemFlag flag = SystemFlag.SubSystem);
    }
}
