using Syadeu.Mono;
using System.Collections.Generic;
using UnityEngine;

using MoonSharp.Interpreter;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Database
{
    [StaticManagerIntializeOnLoad]
    internal sealed class LuaManager : StaticDataManager<LuaManager>
    {
        private Dictionary<string, string> m_Scripts = new Dictionary<string, string>();
        private Script m_MainScripter = new Script();

        public override void OnInitialize()
        {
            LoadScripts();

            ConsoleWindow.CreateCommand((cmd) =>
            {
                LoadScripts();
                ConsoleWindow.Log("y");

                foreach (var item in m_Scripts)
                {
                    DynValue value = m_MainScripter.DoString(item.Value);
                    ConsoleWindow.Log(value.CastToString());
                }
            }, "reload", "lua");
        }

        private void LoadScripts()
        {
            m_Scripts.Clear();

            ConsoleWindow.Log("Loading lua scripts");
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            TextAsset[] result = Resources.LoadAll<TextAsset>("Lua");

            for (int i = 0; i < result.Length; i++)
            {
                ConsoleWindow.Log($"Script {result[i].name} is loaded");
                m_Scripts.Add(result[i].name, result[i].text);
            }
            ConsoleWindow.Log($"Loaded lua scripts : {result.Length}");

            Script.DefaultOptions.ScriptLoader = new MoonSharp.Interpreter.Loaders.UnityAssetsScriptLoader(m_Scripts);
        }
    }
}
