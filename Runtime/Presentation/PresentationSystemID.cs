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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation.Internal;
using System;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 시스템(<seealso cref="PresentationSystemEntity{T}"/>) 을 unmanaged 타입으로 레퍼런스하는 ID 입니다.
    /// </summary>
    public readonly struct PresentationSystemID : IValidation, IEquatable<PresentationSystemID>
    {
        public static readonly PresentationSystemID Null = new PresentationSystemID(Hash.Empty, 0);

        private readonly Hash m_GroupIndex;
        private readonly int m_SystemIndex;

        internal PresentationSystemID(Hash group, int system)
        {
            m_GroupIndex = group;
            m_SystemIndex = system;
        }

        public PresentationSystemEntity System
        {
            get
            {
#if DEBUG_MODE
                if (IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Cannot retrived system. ID is null.");
                    return null;
                }
                else if (!IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Cannot retrived system. ID is invalid.");
                    return null;
                }
#endif

                var g = PresentationManager.Instance.m_PresentationGroups[m_GroupIndex];
                return g.Systems[m_SystemIndex];
            }
        }

        public bool IsNull() => this.Equals(Null);
        public bool IsValid()
        {
#if DEBUG_MODE
            if (m_GroupIndex.IsEmpty() || m_SystemIndex < 0 ||
                !PresentationManager.Instance.m_PresentationGroups.TryGetValue(m_GroupIndex, out var g) ||
                g.Count < m_SystemIndex)
            {
                return false;
            }
#endif
            return true;
        }

        public bool Equals(PresentationSystemID other)
            => m_GroupIndex.Equals(other.m_GroupIndex) && m_SystemIndex.Equals(other.m_SystemIndex);
    }
    /// <inheritdoc cref="PresentationSystemID"/>
    public readonly struct PresentationSystemID<TSystem> : IValidation, IEquatable<PresentationSystemID<TSystem>>
        where TSystem : PresentationSystemEntity
    {
        public static readonly PresentationSystemID<TSystem> Null = new PresentationSystemID<TSystem>(Hash.Empty, 0);

        private readonly Hash m_GroupIndex;
        private readonly int m_SystemIndex;

        internal PresentationSystemID(Hash group, int system)
        {
            m_GroupIndex = group;
            m_SystemIndex = system;
        }

        /// <summary>
        /// 시스템 <typeparamref name="TSystem"/> 의 인스턴스 입니다.
        /// </summary>
        public TSystem System
        {
            get
            {
#if DEBUG_MODE
                if (IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Cannot retrived system {TypeHelper.TypeOf<TSystem>.Name}. ID is null.");
                    return null;
                }
                else if (!IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Cannot retrived system {TypeHelper.TypeOf<TSystem>.Name}. ID is invalid.");
                    return null;
                }
#endif
                var g = PresentationManager.Instance.m_PresentationGroups[m_GroupIndex];
                return (TSystem)g.Systems[m_SystemIndex];
            }
        }

        public bool IsNull() => this.Equals(Null);
        public bool IsValid()
        {
#if DEBUG_MODE
            if (m_GroupIndex.IsEmpty() || m_SystemIndex < 0 || !PresentationManager.Initialized ||
                !PresentationManager.Instance.m_PresentationGroups.TryGetValue(m_GroupIndex, out var g) ||
                g.Systems.Count < m_SystemIndex)
            {
                return false;
            }

            if (!(g.Systems[m_SystemIndex] is TSystem)) return false;
#endif
            return true;
        }

        public bool Equals(PresentationSystemID<TSystem> other)
            => m_GroupIndex.Equals(other.m_GroupIndex) && m_SystemIndex.Equals(other.m_SystemIndex);

        public static implicit operator PresentationSystemID(PresentationSystemID<TSystem> other)
        {
            return new PresentationSystemID(other.m_GroupIndex, other.m_SystemIndex);
        }
    }
}
