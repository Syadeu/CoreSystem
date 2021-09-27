using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Internal;
using System;

namespace Syadeu.Presentation
{
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
                if (IsNull() || !IsValid()) return null;

                var g = PresentationManager.Instance.m_PresentationGroups[m_GroupIndex];
                return g.Systems[m_SystemIndex];
            }
        }

        public bool IsNull() => this.Equals(Null);
        public bool IsValid()
        {
#if UNITY_EDITOR
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
    public readonly struct PresentationSystemID<T> : IValidation, IEquatable<PresentationSystemID<T>>
        where T : PresentationSystemEntity
    {
        public static readonly PresentationSystemID<T> Null = new PresentationSystemID<T>(Hash.Empty, 0);

        private readonly Hash m_GroupIndex;
        private readonly int m_SystemIndex;

        internal PresentationSystemID(Hash group, int system)
        {
            m_GroupIndex = group;
            m_SystemIndex = system;
        }

        public T System
        {
            get
            {
                if (IsNull())
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Cannot retrived system {TypeHelper.TypeOf<T>.Name}. ID is null.");
                    return null;
                }
                else if (!IsValid())
                {
                    CoreSystem.Logger.LogError(Channel.Presentation,
                        $"Cannot retrived system {TypeHelper.TypeOf<T>.Name}. ID is invalid.");
                    return null;
                }

                var g = PresentationManager.Instance.m_PresentationGroups[m_GroupIndex];
                return (T)g.Systems[m_SystemIndex];
            }
        }

        public bool IsNull() => this.Equals(Null);
        public bool IsValid()
        {
#if UNITY_EDITOR
            if (m_GroupIndex.IsEmpty() || m_SystemIndex < 0 || !PresentationManager.Initialized ||
                !PresentationManager.Instance.m_PresentationGroups.TryGetValue(m_GroupIndex, out var g) ||
                g.Systems.Count < m_SystemIndex)
            {
                return false;
            }

            if (!(g.Systems[m_SystemIndex] is T)) return false;
#endif
            return true;
        }

        public bool Equals(PresentationSystemID<T> other)
            => m_GroupIndex.Equals(other.m_GroupIndex) && m_SystemIndex.Equals(other.m_SystemIndex);

        public static implicit operator PresentationSystemID(PresentationSystemID<T> other)
        {
            return new PresentationSystemID(other.m_GroupIndex, other.m_SystemIndex);
        }
    }
}
