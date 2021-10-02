using Syadeu.Database;
using Syadeu.Presentation.Components;
using System;
using Unity.Collections;

namespace Syadeu.Presentation.Actor
{
    public struct ActorFactionComponent : IEntityComponent, IEquatable<ActorFactionComponent>
    {
        internal FactionType m_FactionType;
        internal Hash m_Hash;
        internal FixedList512Bytes<Hash> m_Allies;
        internal FixedList512Bytes<Hash> m_Enemies;

        public FactionType FactionType
        {
            get => m_FactionType;
            set => m_FactionType = value;
        }

        public bool IsAllies(in ActorFactionComponent faction)
        {
            if (Equals(faction)) return true;

            for (int i = 0; i < m_Allies.Length; i++)
            {
                if (m_Allies[i].Equals(faction.m_Hash)) return true;
            }
            return false;
        }
        public bool IsEnemies(in ActorFactionComponent faction)
        {
            if (Equals(faction)) return false;

            for (int i = 0; i < m_Enemies.Length; i++)
            {
                if (m_Enemies[i].Equals(faction.m_Hash)) return true;
            }
            return false;
        }

        public bool Equals(ActorFactionComponent other)
        {
            return m_Hash.Equals(other.m_Hash);
        }
    }
}
