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

namespace Syadeu.Collections
{
    public enum Direction : int
    {
        NONE = 0,

        Up          = 0b000001,
        Down        = 0b000010,
        Left        = 0b000100,
        Right       = 0b001000,
        Forward     = 0b010000,
        Backward    = 0b100000,

        UpDown = Up | Down,
        UpLeft = Up | Left,
        UpRight = Up | Right,
        DownLeft = Up | Left,
        DownRight = Up | Right,

        LeftRight = Left | Right,

        UpLeftDown = Up | Left | Down,
        UpRightDown = Up | Right | Down,
        LeftUpRight = Left | Up | Right,
        LeftDownRight = Left | Down | Right,

        UpRightCorner = Up | Right,
        UpLeftCorner = Up | Left,
        DownRightCorner = Down | Right,
        DownLeftCorner = Down | Left,

        UpDownLeftRight = ~0
    }
}
