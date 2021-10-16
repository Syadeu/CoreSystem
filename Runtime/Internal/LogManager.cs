#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System.Threading;
using System.Collections.Concurrent;

using Syadeu.Mono;
using Syadeu.Collections;

using UnityEngine;
using Unity.Collections;

namespace Syadeu.Internal
{
    internal static class LogManager
    {
        const string c_LogText = "<color={0}>{1}</color>";
        const string c_LogBaseText = "[<color={0}>CoreSystem</color>][{1}][{2}]: {3}";
        const string c_LogAssertText = "[<color={0}>CoreSystem</color>][{1}]: {2}";
        const string c_LogThreadText = "[<color={0}>{1}</color>]";

        const string c_LogThreadErrorText = "This method({0}) is not allowed to use in this thread({1}). Accepts only {2}";

        private enum StringColor
        {
            black,
            blue,
            brown,
            cyan,
            darkblue,
            fuchsia,
            green,
            grey,
            lightblue,
            lime,
            magenta,
            maroon,
            navy,
            olive,
            orange,
            purple,
            red,
            silver,
            teal,
            white,
            yellow
        }

#if DEBUG_MODE
        private static readonly ConcurrentDictionary<Thread, ThreadInfo> m_ThreadInfos = new ConcurrentDictionary<Thread, ThreadInfo>();
#endif
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void RegisterThread(ThreadInfo info, Thread t)
        {
#if DEBUG_MODE
            if (m_ThreadInfos.TryGetValue(t, out ThreadInfo threadInfo))
            {
                m_ThreadInfos[t] = info;
            }
            else m_ThreadInfos.TryAdd(t, info);
#endif
        }
        public static ThreadInfo GetThreadType()
        {
#if DEBUG_MODE
            Thread t = Thread.CurrentThread;
            if (m_ThreadInfos.TryGetValue(t, out ThreadInfo threadInfo))
            {
                return threadInfo;
            }
            return ThreadInfo.User;
#else
            return ThreadInfo.Unity;
#endif
        }

#line hidden
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void ThreadBlock(string name, ThreadInfo acceptOnly)
        {
            if ((acceptOnly & ThreadInfo.Unity) == ThreadInfo.Unity)
            {
                if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                {
                    Log(Channel.Thread, ResultFlag.Error,
                        string.Format(c_LogThreadErrorText, name,
                            TypeHelper.Enum<ThreadInfo>.ToString(ThreadInfo.Unity),
                            TypeHelper.Enum<ThreadInfo>.ToString(acceptOnly)),
                        false);
                }

                return;
            }

            ThreadInfo info = GetThreadType();
            if (!acceptOnly.HasFlag(info))
            {
                Log(Channel.Thread, ResultFlag.Error,
                    string.Format(c_LogThreadErrorText, name,
                        TypeHelper.Enum<ThreadInfo>.ToString(info), 
                        TypeHelper.Enum<ThreadInfo>.ToString(acceptOnly)), 
                    false);
            }
        }
#if DEBUG_MODE
        [System.Diagnostics.DebuggerHidden]
#endif
        public static void Log(Channel channel, ResultFlag result, string msg, bool logThread)
        {
            if (channel != Channel.Editor &&
                ((CoreSystemSettings.Instance.m_DisplayLogChannel | channel) != CoreSystemSettings.Instance.m_DisplayLogChannel))
            {
                if (result == ResultFlag.Normal) return;
            }

            if (result == ResultFlag.Error)
            {
                Log(TypeHelper.Enum<Channel>.ToString(channel), result, msg, logThread);
            }
            else
            {
                LogOnDebug(TypeHelper.Enum<Channel>.ToString(channel), result, msg, logThread);
            }
        }

#if DEBUG_MODE
        [System.Diagnostics.DebuggerHidden]
#endif
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void LogOnDebug(string channel, ResultFlag result, string msg, bool logThread)
            => Log(channel, result, msg, logThread);
#if DEBUG_MODE
        [System.Diagnostics.DebuggerHidden]
#endif
        public static void Log(string channel, ResultFlag result, string msg, bool logThread)
        {
            string text = string.Empty;
            if (logThread)
            {
                text = string.Format(c_LogThreadText, TypeHelper.Enum<StringColor>.ToString(StringColor.fuchsia), GetThreadType());
            }
            
            switch (result)
            {
                case ResultFlag.Warning:
                    text += string.Format(c_LogBaseText, TypeHelper.Enum<StringColor>.ToString(StringColor.lime), 
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.orange), TypeHelper.Enum<ResultFlag>.ToString(result)), 
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.white), channel), 
                        msg);
                    Debug.LogWarning(text);
                    break;
                case ResultFlag.Error:
                    text += string.Format(c_LogBaseText, TypeHelper.Enum<StringColor>.ToString(StringColor.lime), 
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.maroon), TypeHelper.Enum<ResultFlag>.ToString(result)),
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.white), channel),
                        msg);
                    Debug.LogError(text);
                    break;
                default:
                    text += string.Format(c_LogBaseText, TypeHelper.Enum<StringColor>.ToString(StringColor.lime), 
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.teal), TypeHelper.Enum<ResultFlag>.ToString(result)),
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.white), channel),
                        msg);
                    Debug.Log(text);
                    break;
            }
        }

#if DEBUG_MODE
        [System.Diagnostics.DebuggerHidden]
#endif
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void Log(in FixedString128Bytes channel, ResultFlag result, in FixedString128Bytes msg, bool logThread)
        {
            string text = string.Empty;
            if (logThread)
            {
                text = string.Format(c_LogThreadText, TypeHelper.Enum<StringColor>.ToString(StringColor.fuchsia), GetThreadType());
            }

            switch (result)
            {
                case ResultFlag.Warning:
                    text += string.Format(c_LogBaseText, TypeHelper.Enum<StringColor>.ToString(StringColor.lime),
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.orange), TypeHelper.Enum<ResultFlag>.ToString(result)),
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.white), channel),
                        msg);
                    Debug.LogWarning(text);
                    break;
                case ResultFlag.Error:
                    text += string.Format(c_LogBaseText, TypeHelper.Enum<StringColor>.ToString(StringColor.lime),
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.maroon), TypeHelper.Enum<ResultFlag>.ToString(result)),
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.white), channel),
                        msg);
                    Debug.LogError(text);
                    break;
                default:
                    text += string.Format(c_LogBaseText, TypeHelper.Enum<StringColor>.ToString(StringColor.lime),
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.teal), TypeHelper.Enum<ResultFlag>.ToString(result)),
                        string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.white), channel),
                        msg);
                    Debug.Log(text);
                    break;
            }
        }

        private static string AssertText(string msg)
        {
            const string assert = "Assert";
            return 
                string.Format(c_LogAssertText, TypeHelper.Enum<StringColor>.ToString(StringColor.lime), 
                string.Format(c_LogText, TypeHelper.Enum<StringColor>.ToString(StringColor.maroon), assert), msg);
        }

        #region Asserts

        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void Null(object obj, string msg)
        {
            const string defaultMsg = "Object is not null. Expected null";
            if (obj == null) return;
            if (string.IsNullOrEmpty(msg)) msg = defaultMsg;
            else msg += " Expected null";

            Debug.LogError(AssertText(msg));
        }
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void NotNull(object obj, string msg)
        {
            const string defaultMsg = "Object is null. Expected not null";
            if (obj != null) return;
            if (string.IsNullOrEmpty(msg)) msg = defaultMsg;
            else msg += " Expected not null";

            Debug.LogError(AssertText(msg));
        }

        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void True(bool value, string msg)
        {
            const string defaultMsg = "{0} is false. Expected true";
            if (value) return;
            if (string.IsNullOrEmpty(msg)) msg = string.Format(defaultMsg, value);

            Debug.LogError(AssertText(msg));
        }
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void False(bool value, string msg)
        {
            const string defaultMsg = "{0} is true. Expected false";
            if (!value) return;
            if (string.IsNullOrEmpty(msg)) msg = string.Format(defaultMsg, value);

            Debug.LogError(AssertText(msg));
        }

        #endregion
#line default
    }
}
