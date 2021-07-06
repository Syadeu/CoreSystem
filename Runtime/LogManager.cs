using Syadeu.Database;
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
#line hidden
        public static void Log(Channel channel, ResultFlag result, string msg)
        {
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
        public static void NotNull(object obj, string msg)
        {
            const string assert = "Assert";
            const string defaultMsg = "Object {0} is null. Expected not null";
            if (obj != null) return;
            if (string.IsNullOrEmpty(msg)) msg = string.Format(defaultMsg, obj);

            string text = string.Format(c_LogAssertText, StringColor.lime, string.Format(c_LogText, StringColor.maroon, assert), msg);
            Debug.LogError(text);
        }
#line default
    }
    public enum Channel
    {
        None,

        Core = 1 << 0,

        Jobs,
        Lua,
        Scene,
        Presentation,
        Audio,

        All = ~0
    }
}
