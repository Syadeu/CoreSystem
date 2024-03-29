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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif


namespace Syadeu.Presentation.Grid
{
    public sealed class OnGridCellCursorOverlapEvent : SynchronizedEvent<OnGridCellCursorOverlapEvent>
    {
        public GridIndex Index { get; private set; }

        public static OnGridCellCursorOverlapEvent GetEvent(GridIndex index)
        {
            var temp = Dequeue();

            temp.Index = index;

            return temp;
        }
        protected override void OnTerminate()
        {
        }
    }
}
