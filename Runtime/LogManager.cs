﻿using Syadeu.Database;
using UnityEngine;

namespace Syadeu
{
    internal static class LogManager
    {
        const string c_LogText = "<color={0}>{1}</color>";
        const string c_LogBaseText = "[<color={0}>CoreSystem</color>][{1}][{2}]: {3}";
        const string c_LogAssertText = "[<color={0}>CoreSystem</color>][{1}]: {3}";
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
        internal static Channel s_DisplayLogChannel = Channel.All;
#line hidden
        public static void Log(Channel channel, ResultFlag result, string msg)
        {
            if (!s_DisplayLogChannel.HasFlag(channel)) return;

            string text;
            switch (result)
            {
                case ResultFlag.Warning:
                    text = string.Format(c_LogBaseText, StringColor.lime, string.Format(c_LogText, StringColor.orange, result), channel, msg);
                    Debug.LogWarning(text);
                    break;
                case ResultFlag.Error:
                    text = string.Format(c_LogBaseText, StringColor.lime, string.Format(c_LogText, StringColor.maroon, result), channel, msg);
                    Debug.LogError(text);
                    break;
                default:
                    text = string.Format(c_LogBaseText, StringColor.lime, string.Format(c_LogText, StringColor.teal, result), channel, msg);
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
            const string defaultMsg = "Object {0} is not null. Expected null";
            if (obj == null) return;
            if (string.IsNullOrEmpty(msg)) msg = string.Format(defaultMsg, obj);

            Debug.LogError(AssertText(msg));
        }
        public static void NotNull(object obj, string msg)
        {
            const string defaultMsg = "Object {0} is null. Expected not null";
            if (obj != null) return;
            if (string.IsNullOrEmpty(msg)) msg = string.Format(defaultMsg, obj);

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
    public enum Channel
    {
        None = 0,

        Core = 1 << 0,
        Editor = 1 << 1,

        Jobs = 1 << 10,
        Lua = 1 << 11,

        Mono = 1 << 20,
        Creature = 1 << 21,

        Presentation = 1 << 30,
        Scene = 1 << 31,

        Audio = 1 << 40,

        All = ~0
    }
}
