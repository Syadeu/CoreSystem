using Syadeu.Mono;
using System.Collections.Generic;
using UnityEngine;

using MoonSharp.Interpreter;
using System.Linq;
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
            UserData.RegisterType<LuaUtils>();
            UserData.RegisterType<LuaVectorUtils>();
            m_MainScripter.Globals["CoreSystem"] = typeof(LuaUtils);
            m_MainScripter.Globals["Vector"] = typeof(LuaVectorUtils);

            LoadScripts();

            ConsoleWindow.CreateCommand((cmd) =>
            {
                LoadScripts();
                
                foreach (var item in m_Scripts)
                {
                    DynValue value;
                    try
                    {
                        value = m_MainScripter.DoString(item.Value);
                        //ConsoleWindow.Log(value.ToObject().ToString());
                        //ConsoleWindow.Log(value.Table.Values.First().num.ToString());
                    }
                    catch (System.Exception ex)
                    {
                        ConsoleWindow.Log(ex.ToString(), ConsoleFlag.Error);
                    }
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

    internal sealed class LuaUtils
    {
        //public static string ToString(object obj) => obj.ToString();

        //public static bool IsArray(object obj) => obj.GetType().IsArray;
        public static void Log(string txt) => ConsoleWindow.Log(txt);
    }
    internal sealed class LuaVectorUtils
    {
        private static Vector3 GetVector(double[] vs) => new Vector3((float)vs[0], (float)vs[1], (float)vs[2]);
        private static double[] ToVector(Vector3 vec) => new double[] { vec.x, vec.y, vec.z };

        public static double[] ToVector2(float a, float b) => new double[] { a, b };
        public static double[] ToVector3(float a, float b, float c) => new double[] { a, b, c };

        public static double[] Lerp(double[] a, double[] b, float t)
            => ToVector(Vector3.Lerp(GetVector(a), GetVector(b), t));
    }
}
