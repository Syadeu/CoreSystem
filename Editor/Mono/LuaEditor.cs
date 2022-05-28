using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;
using Syadeu;
using Syadeu.Collections;
using Syadeu.Collections.Editor;
using Syadeu.Collections.Lua;
using Syadeu.Internal;
using SyadeuEditor.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SyadeuEditor
{
    public static class LuaEditor
    {
        #region Script Loader
        public sealed class LuaEditorScriptLoader : ScriptLoaderBase
        {
            private readonly Dictionary<string, string> m_Resources;
            public Dictionary<string, string> Resources => m_Resources;

            public LuaEditorScriptLoader()
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
                m_Resources.Clear();

                TextAsset[] scriptAssets = UnityEngine.Resources.LoadAll<TextAsset>("Lua");
                for (int i = 0; i < scriptAssets.Length; i++)
                {
                    m_Resources.Add(scriptAssets[i].name, scriptAssets[i].text);
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

                        LoadAllScripts(folders[i], scrs, depth + 1);
                    }

                    //CoreSystem.Log(Channel.Lua, $"Searching modules at ({path})");
                    string[] scriptsPath = Directory.GetFiles(path);
                    for (int i = 0; i < scriptsPath.Length; i++)
                    {
                        if (!Path.GetExtension(scriptsPath[i]).Equals(".lua")) continue;

                        scrs.Add(GetFileName(scriptsPath[i]), File.ReadAllText(scriptsPath[i]));
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
        #endregion

        private static Script m_EditorScripter;
        private static LuaEditorScriptLoader m_ScriptLoader;

        private static string[] m_FunctionNames;
        private static Type[] m_ArgumentTypes;
        private static string[] m_ArgumentTypeNames;

        static LuaEditor()
        {
            m_EditorScripter = new Script();
            m_ScriptLoader = new LuaEditorScriptLoader();
            m_EditorScripter.Options.ScriptLoader = m_ScriptLoader;

            m_ArgumentTypes = TypeHelper.GetTypes((other) => other.GetCustomAttribute<LuaArgumentTypeAttribute>() != null);
            List<string> temp = new List<string>();
            temp.Add("None");
            for (int i = 0; i < m_ArgumentTypes.Length; i++)
            {
                temp.Add(m_ArgumentTypes[i].Name);
            }
            m_ArgumentTypeNames = temp.ToArray();

            Reload();
        }
        public static void Reload()
        {
            m_ScriptLoader.ReloadScripts();
            foreach (var item in m_ScriptLoader.Resources)
            {
                DynValue value;
                try
                {
                    value = m_EditorScripter.DoString(item.Value);
                }
                catch (ScriptRuntimeException runtimeEx)
                {
                    Debug.LogError(runtimeEx.DecoratedMessage);
                }
                catch (SyntaxErrorException syntaxEx)
                {
                    Debug.LogError(syntaxEx.DecoratedMessage);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            List<string> funcs = new List<string>();
            funcs.Add("None");
            foreach (var item in m_EditorScripter.Globals.Pairs)
            {
                if (item.Value.Type != DataType.Function && item.Value.Type != DataType.UserData) continue;
                if (item.Key.CastToString().Equals("require")) continue;

                funcs.Add(item.Key.CastToString());
            }
            m_FunctionNames = funcs.ToArray();
        }
        public static void DrawGUI(this LuaScriptContainer other, string name)
        {
            const string header = "{0}: <size=10>Lua Function {1}</size>";
            EditorGUILayout.BeginVertical(EditorStyleUtilities.Box);

            EditorGUILayout.BeginHorizontal();
            CoreGUI.Label(string.Format(header, name, other.m_Scripts?.Count.ToString()), 15);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                if (other.m_Scripts == null) other.m_Scripts = new List<LuaScript>();
                other.m_Scripts.Add(string.Empty);
            }
            if (other.m_Scripts != null && other.m_Scripts.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
            {
                other.m_Scripts.RemoveAt(other.m_Scripts.Count - 1);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel += 1;

            for (int i = 0; i < other.m_Scripts?.Count; i++)
            {
                CoreGUI.Line();
                EditorGUILayout.BeginHorizontal();
                CoreGUI.Label(string.IsNullOrEmpty(other.m_Scripts[i].m_FunctionName) ? $"{i}" : other.m_Scripts[i].m_FunctionName, 12);
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    other.m_Scripts.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    continue;
                }
                EditorGUILayout.EndHorizontal();
                DrawLuaScript(other.m_Scripts[i]);
            }

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }
        private static void DrawLuaScript(LuaScript scr)
        {
            int idx = 0;
            if (!string.IsNullOrEmpty(scr.m_FunctionName))
            {
                for (int i = 0; i < m_FunctionNames.Length; i++)
                {
                    if (m_FunctionNames[i].Equals(scr.m_FunctionName))
                    {
                        idx = i;
                        break;
                    }
                }
            }
            int selected = EditorGUILayout.Popup("Function", idx, m_FunctionNames);
            if (selected == 0)
            {
                scr.m_FunctionName = string.Empty;
                scr.m_Args = null;
            }
            else scr.m_FunctionName = m_FunctionNames[selected];

            EditorGUI.BeginDisabledGroup(selected == 0);

            EditorGUILayout.BeginHorizontal();
            CoreGUI.Label("Args", 12);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                if (scr.m_Args == null)
                {
                    scr.m_Args = new List<LuaArg>();
                }
                scr.m_Args.Add(LuaArg.Empty);
            }
            if (scr.m_Args != null && scr.m_Args.Count > 0 && GUILayout.Button("-", GUILayout.Width(20)))
            {
                scr.m_Args.RemoveAt(scr.m_Args.Count - 1);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel += 1;
            for (int i = 0; i < scr.m_Args?.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                int argIdx = GetArgIdx(scr.m_Args[i]);
                int selectArgIdx = EditorGUILayout.Popup($"arg {i}: ", argIdx, m_ArgumentTypeNames);
                if (selectArgIdx == 0) scr.m_Args[i] = LuaArg.Empty;
                else scr.m_Args[i] = m_ArgumentTypes[selectArgIdx - 1];

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel -= 1;

            EditorGUI.EndDisabledGroup();
        }
        public static void DrawFunctionSelector(this LuaScript scr, string name)
        {
            EditorGUILayout.BeginVertical(EditorStyleUtilities.Box);
            if (!string.IsNullOrEmpty(name)) CoreGUI.Label(name, 15);
            EditorGUI.indentLevel += 1;

            DrawLuaScript(scr);

            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }

        private static int GetArgIdx(LuaArg arg)
        {
            for (int i = 0; i < m_ArgumentTypes.Length; i++)
            {
                if (m_ArgumentTypes[i].Equals(arg.Type)) return i + 1;
            }
            return 0;
        }
    }
}
