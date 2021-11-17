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

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public readonly struct RaycastInfo
    {
        public static readonly RaycastInfo Empty = new RaycastInfo(Entity<IEntity>.Empty, false, float.MaxValue, float3.zero);

        public readonly Entity<IEntity> entity;
        public readonly bool hit;
        
        public readonly float distance;
        public readonly float3 point;

        internal RaycastInfo(Entity<IEntity> a, bool b, float c, float3 d)
        {
            entity = a; hit = b; distance = c; point = d;
        }
    }
}
