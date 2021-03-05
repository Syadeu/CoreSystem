using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Console
{
    [CreateAssetMenu(menuName = "Syadeu/Console/Command Definition")]
    public sealed class CommandDefinition : ScriptableObject
    {
        public string m_Initializer = null;
        public CommandSetting m_Settings = CommandSetting.None;

        [Space]
        public List<CommandField> m_Args = new List<CommandField>();

        internal bool Connected { get; set; } = false;
        internal Action<string> Action { get; set; }
        internal CommandRequires Requires { get; set; } = null;

        internal CommandField Find(string cmd)
        {
            for (int i = 0; i < m_Args.Count; i++)
            {
                if (m_Args[i].m_Field == cmd) return m_Args[i];
            }
            return null;
        }
    }
}
