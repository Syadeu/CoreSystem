using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Console
{
    [CreateAssetMenu(menuName = "Syadeu/Console/Command Field")]
    public sealed class CommandField : ScriptableObject
    {
        public string m_Field = null;
        public CommandInputType m_Type = CommandInputType.None;

        [Space]
        public List<CommandField> m_Args = new List<CommandField>();

        public Action<string> Action { get; internal set; }

        public CommandField Find(string cmd)
        {
            for (int i = 0; i < m_Args.Count; i++)
            {
                if (m_Args[i].m_Field == cmd) return m_Args[i];
            }
            return null;
        }
    }
}
