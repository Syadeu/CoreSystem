using System.Threading;
using System.Collections.Concurrent;

using Syadeu.Mono;
using Syadeu.Database;
using UnityEngine;

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
        //internal static Channel s_DisplayLogChannel = Channel.All;

        private static readonly ConcurrentDictionary<Thread, ThreadInfo> m_ThreadInfos = new ConcurrentDictionary<Thread, ThreadInfo>();
        public static void RegisterThread(ThreadInfo info, Thread t)
        {
            if (m_ThreadInfos.TryGetValue(t, out ThreadInfo threadInfo))
            {
                m_ThreadInfos[t] = info;
            }
            else m_ThreadInfos.TryAdd(t, info);
        }
        public static ThreadInfo GetThreadType()
        {
            Thread t = Thread.CurrentThread;
            if (m_ThreadInfos.TryGetValue(t, out ThreadInfo threadInfo))
            {
                return threadInfo;
            }
            return ThreadInfo.User;
        }

#line hidden
        public static void ThreadBlock(string name, ThreadInfo acceptOnly)
        {
            ThreadInfo info = GetThreadType();
            if (!acceptOnly.HasFlag(info))
            {
                Log(Channel.Thread, ResultFlag.Error,
                    string.Format(c_LogThreadErrorText, name, info, acceptOnly), false);
            }
        }
        public static void Log(Channel channel, ResultFlag result, string msg, bool logThread)
        {
            if (!CoreSystemSettings.Instance.m_DisplayLogChannel.HasFlag(channel))
            {
                if (result == ResultFlag.Normal) return;
            }

            string text = string.Empty;
            if (logThread)
            {
                text = string.Format(c_LogThreadText, StringColor.fuchsia, GetThreadType());
            }

            switch (result)
            {
                case ResultFlag.Warning:
                    text += string.Format(c_LogBaseText, StringColor.lime, 
                        string.Format(c_LogText, StringColor.orange, result), 
                        string.Format(c_LogText, StringColor.white, channel), 
                        msg);
                    Debug.LogWarning(text);
                    break;
                case ResultFlag.Error:
                    text += string.Format(c_LogBaseText, StringColor.lime, 
                        string.Format(c_LogText, StringColor.maroon, result),
                        string.Format(c_LogText, StringColor.white, channel),
                        msg);
                    Debug.LogError(text);
                    break;
                default:
                    text += string.Format(c_LogBaseText, StringColor.lime, 
                        string.Format(c_LogText, StringColor.teal, result),
                        string.Format(c_LogText, StringColor.white, channel),
                        msg);
                    Debug.Log(text);
                    break;
            }
        }

        private static string AssertText(string msg)
        {
            const string assert = "Assert";
            return string.Format(c_LogAssertText, StringColor.lime, string.Format(c_LogText, StringColor.maroon, assert), msg);
        }

        #region Asserts
        public static void Null(object obj, string msg)
        {
            const string defaultMsg = "Object is not null. Expected null";
            if (obj == null) return;
            if (string.IsNullOrEmpty(msg)) msg = defaultMsg;
            else msg += " Expected null";

            Debug.LogError(AssertText(msg));
        }
        public static void NotNull(object obj, string msg)
        {
            const string defaultMsg = "Object is null. Expected not null";
            if (obj != null) return;
            if (string.IsNullOrEmpty(msg)) msg = defaultMsg;
            else msg += " Expected not null";

            Debug.LogError(AssertText(msg));
        }

        public static void True(bool value, string msg)
        {
            const string defaultMsg = "{0} is false. Expected true";
            if (value) return;
            if (string.IsNullOrEmpty(msg)) msg = string.Format(defaultMsg, value);

            Debug.LogError(AssertText(msg));
        }
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

    [System.Flags]
    public enum ThreadInfo
    {
        None,

        Unity = 1 << 0,
        Background = 1 << 1,
        Job = 1 << 2,

        User = 1 << 3
    }
}
