
using MoonSharp.Interpreter;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Syadeu.Internal;
using Syadeu.Presentation;
using UnityEngine;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
#if UNITY_EDITOR
#endif

namespace Syadeu.Database.Lua
{
    [Serializable]
    public sealed class LuaScript : IEquatable<LuaScript>, IValidation
    {
        [JsonProperty(Order = 0, PropertyName = "FunctionName")] public string m_FunctionName;
        [JsonProperty(Order = 1, PropertyName = "Args")] public List<LuaArg> m_Args;
        [JsonIgnore] private Closure m_LuaFunction;

        private LuaScript(string name)
        {
            m_FunctionName = name;
        }
        [JsonConstructor]
        public LuaScript() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException"></exception>
        public DynValue Invoke(params object[] args)
        {
            if (string.IsNullOrEmpty(m_FunctionName)) throw new Exception();

            if (m_LuaFunction == null)
            {
                DynValue temp = LuaManager.GetScriptObject(m_FunctionName);
                if (temp.Type != DataType.Function) throw new Exception();

                m_LuaFunction = temp.Function;
            }
            return m_LuaFunction.Call(args);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (!(obj is LuaScript scr) || !scr.Equals(this)) return false;
            return true;
        }
        public bool Equals(LuaScript other)
        {
            if (other is null) return false;
            return other.m_FunctionName.Equals(m_FunctionName);
        }
        public override int GetHashCode() => (int)FNV1a32.Calculate(m_FunctionName);
        public override string ToString() => m_FunctionName;

        public bool IsValid() => !string.IsNullOrEmpty(m_FunctionName);

        public static implicit operator string(LuaScript a) => a.ToString();
        public static implicit operator LuaScript(string a) => new LuaScript(a);
    }
    [Serializable]
    public sealed class LuaArg
    {
        public static LuaArg Empty = new LuaArg(string.Empty);
        private static readonly Dictionary<Type, LuaArg> m_Cached = new Dictionary<Type, LuaArg>();

        [JsonProperty(Order = 0, PropertyName = "Type")] public string m_TypeName;
        [JsonIgnore] private Type m_Type;

        [JsonIgnore] public Type Type
        {
            get
            {
                if (string.IsNullOrEmpty(m_TypeName)) return null;

                if (m_Type == null) m_Type = Type.GetType(m_TypeName);
                return m_Type;
            }
        }

        private LuaArg(string typeName)
        {
            m_TypeName = typeName;
        }
        [JsonConstructor]
        public LuaArg() { }

        public static LuaArg GetArg<T>()
        {
            if (!m_Cached.TryGetValue(TypeHelper.TypeOf<T>.Type, out LuaArg arg))
            {
                arg = new LuaArg(TypeHelper.TypeOf<T>.FullName);
                m_Cached.Add(TypeHelper.TypeOf<T>.Type, arg);
            }
            return arg;
        }
        public static LuaArg GetArg(Type t)
        {
            if (!m_Cached.TryGetValue(t, out LuaArg arg))
            {
                arg = new LuaArg(t.FullName);
                m_Cached.Add(t, arg);
            }
            return arg;
        }

        public static implicit operator Type(LuaArg a) => a.Type;
        public static implicit operator LuaArg(Type a) => GetArg(a);
    }
    [Serializable]
    public sealed class LuaScriptContainer
    {
        const string c_ScriptError = "This container has an invalid lua function({0}). Request ignored.";

        [JsonProperty(Order = 0, PropertyName = "Functions")] public List<LuaScript> m_Scripts;

        public void Invoke(ProxyTransform target)
        {
            for (int i = 0; i < m_Scripts.Count; i++)
            {
                if (m_Scripts[i] == null || !m_Scripts[i].IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Lua, string.Format(c_ScriptError, $"{i}"));
                    continue;
                }

                try
                {
                    LuaScript scr = m_Scripts[i];
                    if (IsContainsUnityArgs(scr.m_Args))
                    {
                        CoreSystem.AddForegroundJob(() => scr.Invoke(ToArgument(target, scr.m_Args)));
                    }
                    else scr.Invoke(ToArgument(target, scr.m_Args));
                }
                catch (ScriptRuntimeException runtimeEx)
                {
                    CoreSystem.Logger.LogWarning(Channel.Lua, string.Format(c_ScriptError, $"{i}: {runtimeEx.DecoratedMessage}"));
                }
                catch (Exception)
                {
                    throw;
                }
            }

            static bool IsContainsUnityArgs(IList<LuaArg> args)
            {
                if (args == null) return false;
                for (int i = 0; i < args.Count; i++)
                {
                    if (TypeHelper.TypeOf<UnityEngine.Object>.Type.IsAssignableFrom(args[i].Type))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        List<object> ToArgument(ProxyTransform dataObj, IList<LuaArg> args)
        {
            if (args == null || args.Count == 0) return null;
            List<object> temp = new List<object>();
            for (int i = 0; i < args.Count; i++)
            {
                if (TypeHelper.TypeOf<UnityEngine.Object>.Type.IsAssignableFrom(args[i].Type))
                {
                    if (!dataObj.hasProxy) temp.Add(null);
                    else temp.Add(dataObj.proxy.GetComponent(args[i].Type));
                }
                else if (args[i].Type.Equals(TypeHelper.TypeOf<ProxyTransform>.Type))
                {
                    temp.Add(dataObj);
                }
                else if (TypeHelper.TypeOf<IEntity>.Type.IsAssignableFrom(args[i].Type))
                {
                    throw new NotImplementedException();
                    //IEntity entity = dataObj.GetEntity();
                    //temp.Add(entity.GetAttribute(args[i].Type));
                }
                else throw new NotImplementedException($"{args[i].Type.Name}");
            }
            return temp;
        }
    }

    /// <summary>
    /// <see cref="LuaArg"/>
    /// </summary>
    public sealed class LuaArgumentTypeAttribute : Attribute
    {
    }
}
