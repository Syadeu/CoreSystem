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

namespace Syadeu.Collections
{
    [Flags]
    public enum UserTagFlag
    {
        NONE = 0,

        UserTag1 = 0x01,
        UserTag2 = 0x02,
        UserTag3 = 0x03,
        UserTag4 = 0x04,
        UserTag5 = 0x05,
        UserTag6 = 0x06,
        UserTag7 = 0x07,
        UserTag8 = 0x08,
        UserTag9 = 0x09,
        UserTag10 = 0x10,
        UserTag11 = 0x11,
        UserTag12 = 0x12,
        UserTag13 = 0x13,
        UserTag14 = 0x14,
        UserTag15 = 0x15,
        UserTag16 = 0x16,
        UserTag17 = 0x17,
        UserTag18 = 0x18,
        UserTag19 = 0x19,
        UserTag20 = 0x20,
        UserTag21 = 0x21,
        UserTag22 = 0x22,
        UserTag23 = 0x23,
        UserTag24 = 0x24,
        UserTag25 = 0x25,
        UserTag26 = 0x26,
        UserTag27 = 0x27,
        UserTag28 = 0x28,
        UserTag29 = 0x29,
        UserTag30 = 0x30,
        UserTag31 = 0x31,
        UserTag32 = 0x32,

        All = ~0
    }
}
