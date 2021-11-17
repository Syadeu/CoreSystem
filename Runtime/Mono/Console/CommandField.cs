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
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Console
{
    [CreateAssetMenu(menuName = "CoreSystem/Console/Command Field")]
    public sealed class CommandField : ScriptableObject
    {
        public string m_Field = null;
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
                if (m_Args[i].m_Field.Equals(cmd))
                {
                    if (Requires != null)
                    {
                        if (Requires.Invoke()) return m_Args[i];
                        else return null;
                    }
                    else return m_Args[i];
                }
            }
            return null;
        }
    }
}
