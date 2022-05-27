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
    public enum Alignment : short
    {
        None = 0,

        Left = 0b0001,
        Right = 0b0010,
        Down = 0b0100,
        Up = 0b1000,

        Center = Left | Right | Down | Up,
        DownLeft = Down | Left,
    }
}
