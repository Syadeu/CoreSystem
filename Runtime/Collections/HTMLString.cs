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


namespace Syadeu.Collections
{
    public sealed class HTMLString
    {
        public static void AutoString(ref string original, in string txt)
        {
            if (!string.IsNullOrEmpty(original)) original += "\n";
            original += txt;
        }

        public static string String(in string text, StringColor color) => $"<color={TypeHelper.Enum<StringColor>.ToString(color)}>{text}</color>";
        public static string String(in string text, int size) => $"<size={size}>{text}</size>";
        public static string String(in string text, StringColor color, int size) => String(String(text, color), size);
    }
}
