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
using Unity.Burst;

namespace Syadeu.Presentation.Proxy
{
    [BurstCompile(CompileSynchronously = true)]
    public readonly struct ClusterID : IEquatable<ClusterID>
    {
        public static readonly ClusterID Empty = new ClusterID(-1, -1);
        public static readonly ClusterID Requested = new ClusterID(-2, -2);

        private readonly int m_GroupIndex;
        private readonly int m_ItemIndex;

        internal int GroupIndex => m_GroupIndex;
        internal int ItemIndex => m_ItemIndex;

        public ClusterID(int gIdx, int iIdx) { m_GroupIndex = gIdx; m_ItemIndex = iIdx; }

        public bool Equals(ClusterID other) => m_GroupIndex.Equals(other.m_GroupIndex) && m_ItemIndex.Equals(other.m_ItemIndex);
    }
}
