using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Console
{
    [CreateAssetMenu(menuName = "Syadeu/Console/Command/Field")]
    public sealed class CommandField : CommandBase
    {
        public string m_Field;
        public bool m_IncludeNoArg = false;

        [Space]
        public List<CommandBase> m_Args;
    }
}
