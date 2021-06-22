using Syadeu.Mono;
using System.Collections.Generic;
using UnityEngine;

using MoonSharp.Interpreter;
using System.Linq;
using MoonSharp.Interpreter.Loaders;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Database.Lua
{
    public sealed class LuaManager : StaticDataManager<LuaManager>
    {
        private Script m_MainScripter;
        private LuaScriptLoader m_ScriptLoader;

        public override void OnInitialize()
        {
            Debug.Log("LUA: Initialize start");
            Debug.Log("LUA: Registering Proxies");
            UserData.RegisterProxyType<ItemProxy, Item>(r => r.GetProxy());
            UserData.RegisterProxyType<ItemTypeProxy, ItemType>(r => r.GetProxy());
            UserData.RegisterProxyType<ItemEffectTypeProxy, ItemEffectType>(r => r.GetProxy());
            UserData.RegisterProxyType<CreatureBrainProxy, CreatureBrain>(r => r.Proxy);

            Debug.Log("LUA: Registering Actions");
            RegisterSimpleAction();
            RegisterSimpleAction<CreatureBrainProxy>();

            Debug.Log("LUA: Registering Script and Globals");
            m_MainScripter = new Script();
            AddGlobal<LuaUtils>("CoreSystem");
            AddGlobal<LuaVectorUtils>("Vector");
            AddGlobal<LuaItemUtils>("Items");
            AddGlobal<LuaCreatureUtils>("Creature");

            Debug.Log("LUA: Registering ScriptLoader");
            m_ScriptLoader = new LuaScriptLoader();
            m_MainScripter.Options.ScriptLoader = m_ScriptLoader;

            Debug.Log("LUA: Load Scripts");
            LoadScripts();
            Debug.Log("LUA: Creating Console Commands");
            CreateLuaCommands();
        }

        private void CreateLuaCommands()
        {
            ConsoleWindow.CreateCommand((cmd) =>
            {
                LoadScripts();
            }, "lua", "reload");

            ConsoleWindow.CreateCommand((cmd) =>
            {
                $"Displaying all lua functions".ToLogConsole();
                foreach (var item in m_MainScripter.Globals.Pairs)
                {
                    $"{item.Key.CastToString()} : {item.Value.Type}".ToLogConsole(1);
                }
            }, "lua", "get", "functions");
            ConsoleWindow.CreateCommand((cmd) =>
            {
                try
                {
                    if (cmd.Contains('('))
                    {
                        string[] vs = cmd.Split('(');
                        vs[1] = vs[1].Trim(')');

                        string[] parameters = vs[1].Split(',');
                        m_MainScripter.Call(m_MainScripter.Globals[vs[0]], parameters);
                    }
                    else m_MainScripter.Call(m_MainScripter.Globals[cmd]);
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
            }, "lua", "excute");
            //foreach (var item in m_MainScripter.Globals.Pairs)
            //{
            //    if (item.Value.Type != DataType.Function || item.Key.CastToString().Equals("require")) continue;

            //    string[] parameters = new string[] { item.Key.ToString(), item.Value.ToString() };
            //    ConsoleWindow.CreateCommand((cmd) =>
            //    {
            //        m_MainScripter.Call(item.Value);
            //    }, parameters);

            //    $"{parameters[0]}.{parameters[1]} : {item.Key.Type}.{item.Value.Type} added".ToLog();
            //}
        }
        private void LoadScripts()
        {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            m_ScriptLoader.ReloadScripts();
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
        }

        public void AddGlobal(string functionName, Type type)
        {
            UserData.RegisterType(type);
            m_MainScripter.Globals[functionName] = type;
        }
        public void AddGlobal<T>(string functionName) => AddGlobal(functionName, typeof(T));

        public static void RegisterSimpleFunc<T>()
        {
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Function, typeof(Func<T>),
                v =>
                {
                    var function = v.Function;
                    return (Func<T>)(() => function.Call().ToObject<T>());
                }
            );
        }
        public static void RegisterSimpleFunc<T1, TResult>()
        {
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Function, typeof(Func<T1, TResult>),
                v =>
                {
                    var function = v.Function;
                    return (Func<T1, TResult>)((T1 p1) => function.Call(p1).ToObject<TResult>());
                }
            );
        }
        public static void RegisterSimpleAction<T>()
        {
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Function, typeof(Action<T>),
                v =>
                {
                    var function = v.Function;
                    return (Action<T>)(p => function.Call(p));
                }
            );
        }
        public static void RegisterSimpleAction()
        {
            Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Function, typeof(Action),
                v =>
                {
                    var function = v.Function;
                    return (Action)(() => function.Call());
                }
            );
        }
    }

    internal sealed class LuaScriptLoader : ScriptLoaderBase
    {
        private readonly Dictionary<string, string> m_Resources;
        public Dictionary<string, string> Resources => m_Resources;

        /// <summary>
		/// The default path where scripts are meant to be stored (if not changed)
		/// </summary>
		public const string DEFAULT_PATH = "../CoreSystem/Modules/Lua";

        public LuaScriptLoader()
        {
            m_Resources = new Dictionary<string, string>();

            Initialize();
        }

        private void Initialize()
        {
            IgnoreLuaPathGlobal = true;
            ModulePaths = UnpackStringPaths(
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
            "LUA: Reloading".ToLog();
            m_Resources.Clear();

            TextAsset[] scriptAssets = UnityEngine.Resources.LoadAll<TextAsset>("Lua");
            for (int i = 0; i < scriptAssets.Length; i++)
            {
                m_Resources.Add(scriptAssets[i].name, scriptAssets[i].text);
                $"Loaded {scriptAssets[i].name}".ToLog();
            }

            if (!Directory.Exists($"{Application.dataPath}/{DEFAULT_PATH}"))
            {
                Directory.CreateDirectory($"{Application.dataPath}/{DEFAULT_PATH}");
            }
            LoadAllScripts($"{Application.dataPath}/{DEFAULT_PATH}", m_Resources, 1);

            void LoadAllScripts(string path, Dictionary<string, string> scrs, int depth)
            {
                string[] folders = Directory.GetDirectories(path);
                for (int i = 0; i < folders.Length; i++)
                {
                    $"Searching folder ({folders[i]})".ToLog();
                    LoadAllScripts(folders[i], scrs, depth + 1);
                }

                $"Searching modules at ({path})".ToLog();
                string[] scriptsPath = Directory.GetFiles(path);
                for (int i = 0; i < scriptsPath.Length; i++)
                {
                    scrs.Add(GetFileName(scriptsPath[i]), File.ReadAllText(scriptsPath[i]));
                    $"Loaded {GetFileName(scriptsPath[i])}".ToLog();
                }
            }
        }
        public override object LoadFile(string file, Table globalContext)
        {
            //$"requesting {file}".ToLog();
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
            //$"in {modname}".ToLog();

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

        public static float DeltaTime => Time.deltaTime;
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
    internal sealed class LuaItemUtils
    {
        public static ItemProxy GetItem(string guid)
        {
            Item item = ItemDataList.Instance.GetItem(guid);
            if (item == null) return null;
            else
            {
                return item.GetProxy();
            }
        }
    }
    internal sealed class LuaCreatureUtils
    {
        public static Action<CreatureBrainProxy> OnVisible { get; set; }
        public static Action<CreatureBrainProxy> OnInvisible { get; set; }
    }
}
