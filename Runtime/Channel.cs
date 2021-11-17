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
    [System.Flags]
    public enum Channel
    {
        None = 0,

        Core            = 0b000000000000000000000000001,
        Editor          = 0b000000000000000000000000010,
        Jobs            = 0b000000000000000000000000100,

        Lua             = 0b000000000000000000000001000,

        Mono            = 0b000000000000000000000010000,
        Data            = 0b000000000000000000000100000,

        Presentation    = 0b000000000000000000001000000,
        Scene           = 0b000000000000000000010000000,
        Entity          = 0b000000000000000000100000000,
        Proxy           = 0b000000000000000001000000000,
        Render          = 0b000000000000000010000000000,
        Event           = 0b000000000000000100000000000,
        Action          = 0b000000000000001000000000000,
        Component       = 0b000000000000010000000000000,

        Audio           = 0b000000000000100000000000000,

        GC              = 0b001000000000000000000000000,
        Thread          = 0b010000000000000000000000000,
        Debug           = 0b100000000000000000000000000,

        All = ~0
    }
}
