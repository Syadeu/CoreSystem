using Syadeu.Extentions.EditorUtils;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Console
{
    [CreateAssetMenu(menuName = "Syadeu/Console/Command Definition")]
    public sealed class CommandDefinition : ScriptableObject
    {
        public string m_Initializer;
        public bool m_IncludeNoArg = false;

        [Space]
        public List<CommandBase> m_Args;

        public bool Run(string[] args)
        {
            "in".ToLog();
            return true;
        }
        public void Look(ref List<string> fields, ref List<string> inputs)
        {
            fields.Clear(); inputs.Clear();

            for (int i = 0; i < m_Args.Count; i++)
            {
                if (m_Args[i] is CommandField field)
                {
                    fields.Add(field.m_Field);
                }
                else if (m_Args[i] is CommandInput input)
                {
                    inputs.AddRange(input.m_PreDefinedInputs);
                }
            }
        }
    }
}
