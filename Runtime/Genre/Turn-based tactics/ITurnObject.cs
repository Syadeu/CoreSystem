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

namespace Syadeu.Presentation.TurnTable
{
    [Obsolete("", true)]
    public interface ITurnObject
    {
        string DisplayName { get; }
        /// <summary>
        /// <see langword="true"/> 일 경우, 다음 턴 테이블에 이 플레이어를 참여시킵니다
        /// </summary>
        bool ActivateTurn { get; }

        /// <summary>
        /// 값이 낮을 수록, 턴이 일찍 시작됩니다.
        /// </summary>
        float TurnSpeed { get; }

        /// <summary>
        /// 내 턴이 시작할때 실행되는 메소드
        /// </summary>
        void StartTurn();
        /// <summary>
        /// 내 턴이 끝났을때 실행되는 메소드
        /// </summary>
        void EndTurn();
        /// <summary>
        /// 모든 플레이어의 턴이 끝나고 테이블이 초기화 될 때 실행되는 메소드
        /// </summary>
        void ResetTurnTable();
    }
}

