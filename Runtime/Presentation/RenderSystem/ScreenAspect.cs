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

using UnityEngine;

namespace Syadeu.Presentation.Render
{
    public readonly struct ScreenAspect
    {
        public readonly int
            WidthRatio, HeightRatio,
            Width, Heigth;

        public ScreenAspect(Resolution resolution)
        {
            Width = resolution.width;
            Heigth = resolution.height;

            WidthRatio = resolution.width / 80;
            HeightRatio = resolution.height / 80;
        }

        public bool Is16p9()
        {
            return WidthRatio == 16 && HeightRatio == 9;
        }
        public bool Is16p10()
        {
            return WidthRatio == 16 && HeightRatio == 10;
        }
    }
}
