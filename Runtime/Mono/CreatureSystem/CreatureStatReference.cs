using System;
using UnityEngine;

namespace Syadeu.Mono.Creature
{
    [PreferBinarySerialization]
    public sealed class CreatureStatReference : ScriptableObject
    {
        [Serializable]
        public class Value
        {
            public string m_Name;
            public Index m_Idx;

            public object m_InitValue;
            public object m_Value;

            // 0 = int
            // 1 = float
            // 2 = bool
            public int m_ValueType = 0;
        }
        [Serializable]
        public class Index
        {
            public string m_Name;
            /// <summary>
            /// <see cref="CreatureStatDataAttribute.Idx"/>
            /// </summary>
            public int m_Idx;
        }

        [SerializeField] private Value[] m_Values;

        public Value[] Values => m_Values;
    }
}
