using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Console
{
    [CreateAssetMenu(menuName = "Syadeu/Console/Command/Input")]
    public sealed class CommandInput : CommandBase
    {
        public CommandInputType m_Type;
        public List<string> m_PreDefinedInputs;
    }
}
