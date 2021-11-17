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

namespace Syadeu.Presentation.Events
{
    /// <summary>
    /// 시스템을 순차 수행 대상으로 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 사용자는 <seealso cref="EventSystem.TakeQueueTicket{TSystem}(TSystem)"/> 를 통해 다음 순차 수행 대상을
    /// 선언할 수 있습니다.
    /// </remarks>
    public interface ISystemEventScheduler
    {
        PresentationSystemID SystemID { get; }

        /// <summary>
        /// 수행 대상이 되었을 때 실행하는 메소드입니다.
        /// </summary>
        /// <returns></returns>
        void Execute(ScheduledEventHandler handler);
    }
}
