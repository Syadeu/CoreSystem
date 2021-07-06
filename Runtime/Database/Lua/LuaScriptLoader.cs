using System.Collections.Generic;
using UnityEngine;

using MoonSharp.Interpreter;
using System.Linq;
using MoonSharp.Interpreter.Loaders;
using System.IO;

namespace Syadeu.Database.Lua
{
    internal sealed class LuaScriptLoader : ScriptLoaderBase
    {
        private readonly Dictionary<string, string> m_Resources;
        public Dictionary<string, string> Resources => m_Resources;

        public LuaScriptLoader()
        {
            m_Resources = new Dictionary<string, string>();

            Initialize();
        }

        private void Initialize()
        {
            IgnoreLuaPathGlobal = true;
            ModulePaths = UnpackStringPaths(
                $"{CoreSystemFolder.LuaPath}/?;" +
                $"{CoreSystemFolder.LuaPath}/?.lua");
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
            CoreSystem.Log(Channel.Lua, "Reloading");
            m_Resources.Clear();

            TextAsset[] scriptAssets = UnityEngine.Resources.LoadAll<TextAsset>("Lua");
            for (int i = 0; i < scriptAssets.Length; i++)
            {
                m_Resources.Add(scriptAssets[i].name, scriptAssets[i].text);
                CoreSystem.Log(Channel.Lua, $"Loaded {scriptAssets[i].name}");
            }

            if (!Directory.Exists($"{CoreSystemFolder.LuaPath}"))
            {
                Directory.CreateDirectory($"{CoreSystemFolder.LuaPath}");
            }
            LoadAllScripts($"{CoreSystemFolder.LuaPath}", m_Resources, 1);

            void LoadAllScripts(string path, Dictionary<string, string> scrs, int depth)
            {
                string[] folders = Directory.GetDirectories(path);
                for (int i = 0; i < folders.Length; i++)
                {
                    string folderName = Path.GetFileName(folders[i]);
                    if (IsSpecialFolder(folderName)) continue;

                    CoreSystem.Log(Channel.Lua, $"Searching folder ({folderName})");
                    LoadAllScripts(folders[i], scrs, depth + 1);
                }

                //CoreSystem.Log(Channel.Lua, $"Searching modules at ({path})");
                string[] scriptsPath = Directory.GetFiles(path);
                for (int i = 0; i < scriptsPath.Length; i++)
                {
                    if (!Path.GetExtension(scriptsPath[i]).Equals(".lua")) continue;

                    scrs.Add(GetFileName(scriptsPath[i]), File.ReadAllText(scriptsPath[i]));
                    CoreSystem.Log(Channel.Lua, $"Loaded {GetFileName(scriptsPath[i])}.lua");
                }
            }
            bool IsSpecialFolder(string folderName)
            {
                if (folderName.Equals(".vs") || folderName.Equals("v16")) return true;
                return false;
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
your own IScriptLoader (possibly extending ScriptLoaderBase).", file, CoreSystemFolder.LuaPath);

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
}
