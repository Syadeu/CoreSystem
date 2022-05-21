using Syadeu.Mono;
using UnityEngine;

using MoonSharp.Interpreter;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Collections.Lua
{
    [UnityEngine.AddComponentMenu("")]
    public sealed class LuaManager : StaticDataManager<LuaManager>
    {
        private Script m_MainScripter;
        private LuaScriptLoader m_ScriptLoader;

        #region Initializer
        public override void OnInitialize()
        {
            //Debug.Log("LUA: Initialize start");
            CoreSystem.Logger.Log(LogChannel.Lua, "Registering Proxies");
            //UserData.RegisterProxyType<ItemProxy, Item>(r => r.GetProxy());
            //UserData.RegisterProxyType<ItemInstanceProxy, ItemInstance>(r => r.GetLuaProxy());
            //UserData.RegisterProxyType<ItemTypeProxy, ItemType>(r => r.GetProxy());
            //UserData.RegisterProxyType<ItemUseableTypeProxy, ItemUseableType>(r => r.GetProxy());
            //UserData.RegisterProxyType<ItemEffectTypeProxy, ItemEffectType>(r => r.GetProxy());
            //UserData.RegisterProxyType<CreatureBrainProxy, CreatureBrain>(r => r.Proxy);

            CoreSystem.Logger.Log(LogChannel.Lua, "Registering Actions");
            RegisterSimpleAction();
            RegisterSimpleAction<string>();
            //RegisterSimpleAction<CreatureBrain>();
            //RegisterSimpleAction<ItemInstance>();

            CoreSystem.Logger.Log(LogChannel.Lua, "Registering Script and Globals");
            m_MainScripter = new Script();
            AddGlobal<LuaUtils>("CoreSystem");
            AddGlobal<LuaVectorUtils>("Vector");
            //AddGlobal<LuaItemUtils>("Items");
            //AddGlobal<LuaCreatureUtils>("Creature");
            AddGlobal<RandomUtils>("Random");

            CoreSystem.Logger.Log(LogChannel.Lua, "Registering ScriptLoader");
            m_ScriptLoader = new LuaScriptLoader();
            m_MainScripter.Options.ScriptLoader = m_ScriptLoader;

            CoreSystem.Logger.Log(LogChannel.Lua, "Load Scripts");
            LoadScripts();
            CoreSystem.Logger.Log(LogChannel.Lua, "Creating Console Commands");
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
                    if (item.Value.Type != DataType.Function && item.Value.Type != DataType.UserData) continue;
                    if (item.Key.CastToString().Equals("require")) continue;

                    $"{item.Key.CastToString()} : {item.Value.Type}".ToLogConsole(1);
                }
            }, "lua", "get", "functions");
            ConsoleWindow.CreateCommand((cmd) =>
            {
                try
                {
                    m_MainScripter.DoString(cmd);
                }
                catch (ScriptRuntimeException runtimeEx)
                {
                    ConsoleWindow.Log(runtimeEx.DecoratedMessage, ResultFlag.Error);
                }
                catch (SyntaxErrorException syntaxEx)
                {
                    ConsoleWindow.Log(syntaxEx.DecoratedMessage, ResultFlag.Error);
                }
                catch (System.Exception ex)
                {
                    ConsoleWindow.Log(ex.ToString(), ResultFlag.Error);
                }
            }, "lua", "excute");
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
                    ConsoleWindow.Log(runtimeEx.DecoratedMessage, ResultFlag.Error);
                }
                catch (SyntaxErrorException syntaxEx)
                {
                    ConsoleWindow.Log(syntaxEx.DecoratedMessage, ResultFlag.Error);
                }
                catch (System.Exception ex)
                {
                    ConsoleWindow.Log(ex.ToString(), ResultFlag.Error);
                }
            }
        }
        #endregion

        #region Sets
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
        #endregion

        public static DynValue GetScriptObject(string key) => Instance.m_MainScripter.Globals.Get(key);
    }
}
