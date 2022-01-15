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

using Unity.Collections;
using Unity.Mathematics;

namespace Syadeu.Presentation.Render
{
    [BurstCompatible]
    public struct Anchors
    {
        public static Anchors MiddleCenter => new Anchors(.5f, .5f, .5f, .5f);

        public float2 min;
        public float2 max;

        #region Constructors

        public Anchors(float2 min, float2 max)
        {
            this.min = min;
            this.max = max;
        }
        public Anchors(float4 minMax)
        {
            this.min = minMax.xy;
            this.max = minMax.zw;
        }
        public Anchors(float minX, float minY, float maxX, float maxY)
        {
            this.min = new float2(minX, minY);
            this.max = new float2(maxX, maxY);
        }

        #endregion
    }
}
