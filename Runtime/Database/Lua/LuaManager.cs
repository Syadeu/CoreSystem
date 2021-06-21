using Syadeu.Mono;
using System.Collections.Generic;
using UnityEngine;

using MoonSharp.Interpreter;
using System.Linq;
using MoonSharp.Interpreter.Loaders;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Database
{
    internal sealed class LuaManager : StaticDataManager<LuaManager>
    {
        private Script m_MainScripter = new Script();
        private LuaScriptLoader m_ScriptLoader;

        public override void OnInitialize()
        {
            UserData.RegisterType<LuaUtils>();
            UserData.RegisterType<LuaVectorUtils>();
            m_MainScripter.Globals["CoreSystem"] = typeof(LuaUtils);
            m_MainScripter.Globals["Vector"] = typeof(LuaVectorUtils);

            m_ScriptLoader = new LuaScriptLoader();
            m_MainScripter.Options.ScriptLoader = m_ScriptLoader;

            ConsoleWindow.CreateCommand((cmd) =>
            {
                LoadScripts();
                
                foreach (var item in m_ScriptLoader.Resources)
                {
                    DynValue value;
                    try
                    {
                        value = m_MainScripter.DoString(item.Value);
                    }
                    catch (ScriptRuntimeException runtimeEx)
                    {
                        ConsoleWindow.Log(runtimeEx.DecoratedMessage, ConsoleFlag.Error);
                    }
                    catch (SyntaxErrorException syntaxEx)
                    {
                        ConsoleWindow.Log(syntaxEx.DecoratedMessage, ConsoleFlag.Error);
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
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            m_ScriptLoader.ReloadScripts();

            //for (int i = 0; i < result.Length; i++)
            //{
            //    ConsoleWindow.Log($"Script {result[i].name} is loaded");
            //    m_Scripts.Add(result[i].name, result[i].text);
            //}
            //ConsoleWindow.Log($"Loaded lua scripts : {result.Length}");

            //Script.DefaultOptions.ScriptLoader = new MoonSharp.Interpreter.Loaders.UnityAssetsScriptLoader(m_Scripts);
        }
    }

    internal sealed class LuaScriptLoader : ScriptLoaderBase
    {
        private readonly Dictionary<string, string> m_Resources;
        public Dictionary<string, string> Resources => m_Resources;

        /// <summary>
		/// The default path where scripts are meant to be stored (if not changed)
		/// </summary>
		public const string DEFAULT_PATH = "../Modules/Lua";

        public LuaScriptLoader()
        {
            m_Resources = new Dictionary<string, string>();

            Initialize();
            ReloadScripts();
        }

        private void Initialize()
        {
            IgnoreLuaPathGlobal = true;
            ModulePaths = UnpackStringPaths(
                $"?.lua" +
                $"{Application.dataPath}/{DEFAULT_PATH}/?;" +
                $"{Application.dataPath}/{DEFAULT_PATH}/?.lua");
        }
        private static string GetFileName(string filename)
        {
            int b = System.Math.Max(filename.LastIndexOf('\\'), filename.LastIndexOf('/'));

            if (b > 0) filename = filename.Substring(b + 1);
            filename = filename.Replace(".lua", "");

            return filename;
        }

        public void ReloadScripts()
        {
            "LUA: Reloading".ToLogConsole();
            m_Resources.Clear();

            TextAsset[] scriptAssets = UnityEngine.Resources.LoadAll<TextAsset>("Lua");
            for (int i = 0; i < scriptAssets.Length; i++)
            {
                m_Resources.Add(scriptAssets[i].name, scriptAssets[i].text);
                $"Loaded {scriptAssets[i].name}".ToLogConsole(1);
            }

            LoadAllScripts($"{Application.dataPath}/{DEFAULT_PATH}", m_Resources, 1);

            void LoadAllScripts(string path, Dictionary<string, string> scrs, int depth)
            {
                string[] folders = Directory.GetDirectories(path);
                for (int i = 0; i < folders.Length; i++)
                {
                    $"Searching folder ({folders[i]})".ToLogConsole(depth);
                    LoadAllScripts(folders[i], scrs, depth + 1);
                }

                $"Searching modules at ({path})".ToLogConsole(depth);
                string[] scriptsPath = Directory.GetFiles(path);
                for (int i = 0; i < scriptsPath.Length; i++)
                {
                    scrs.Add(GetFileName(scriptsPath[i]), File.ReadAllText(scriptsPath[i]));
                    $"Loaded {GetFileName(scriptsPath[i])}".ToLogConsole(depth + 1);
                }
            }
        }
        public override object LoadFile(string file, Table globalContext)
        {
            $"requesting {file}".ToLog();
            file = GetFileName(file);

            if (m_Resources.ContainsKey(file))
                return m_Resources[file];
            else
            {
                var error = string.Format(
@"Cannot load script '{0}'. By default, scripts should be .txt files placed under a Assets/Resources/{1} directory.
If you want scripts to be put in another directory or another way, use a custom instance of UnityAssetsScriptLoader or implement
your own IScriptLoader (possibly extending ScriptLoaderBase).", file, DEFAULT_PATH);

                throw new System.Exception(error);
            }
        }
        public override bool ScriptFileExists(string file)
        {
            file = GetFileName(file);
            return m_Resources.ContainsKey(file);
        }
        public override string ResolveModuleName(string modname, Table globalContext)
        {
            $"in {modname}".ToLog();

            if (m_Resources.ContainsKey(modname)) return modname;
            else return base.ResolveModuleName(modname, globalContext);
        }
        /// <summary>
		/// Gets the list of loaded scripts filenames (useful for debugging purposes).
		/// </summary>
		/// <returns></returns>
		public string[] GetLoadedScripts()
        {
            return m_Resources.Keys.ToArray();
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
