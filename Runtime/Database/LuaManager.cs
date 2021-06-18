using Syadeu.Mono;
using System.Collections.Generic;
using UnityEngine;

using MoonSharp.Interpreter;

namespace Syadeu.Database
{
    [StaticManagerIntializeOnLoad]
    internal sealed class LuaManager : StaticDataManager<LuaManager>
    {
        private Dictionary<string, string> m_Scripts = new Dictionary<string, string>();

        public override void OnInitialize()
        {
            LoadScripts();

            ConsoleWindow.CreateCommand((cmd) =>
            {
                ConsoleWindow.Log("y");
            }, "reload", "lua");
        }

        private void LoadScripts()
        {
            TextAsset[] result = Resources.LoadAll<TextAsset>("");
            for (int i = 0; i < result.Length; i++)
            {
                m_Scripts.Add(result[i].name, result[i].text);
            }

            Script.DefaultOptions.ScriptLoader = new MoonSharp.Interpreter.Loaders.UnityAssetsScriptLoader(m_Scripts);
        }
    }
}
