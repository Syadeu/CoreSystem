// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;

namespace Syadeu.Collections
{
    public static class CoreHelper
    {
        internal const string
            c_WhiteSpace = " ",
            c_MessagePrefix = "[<color=lime>CoreSystem</color>]",
            c_Context = "[{0}]",
            c_InvalidString = "Invalid";

        #region Log

#line hidden

        //internal static LowLevel.LogHandler s_LogHandler;
#if UNITY_EDITOR
        /// <summary>
        /// Editor Only
        /// </summary>
        public static string s_EditorLogs = string.Empty;
#endif
        private const string c_LogChannelKey = "CoreSystem_LogChannel";

        public static LogChannel LogChannel
        {
            get => (LogChannel)PlayerPrefs.GetInt(c_LogChannelKey);
            set => PlayerPrefs.SetInt(c_LogChannelKey, (int)value);
        }

        private static string LogStringFormat(LogChannel channel, in string msg, int type)
        {
            string chan = TypeHelper.Enum<LogChannel>.ToString(channel);

            string context;
            switch (type)
            {
                // norm
                default:
                case 0:
                    context = string.Format(c_Context, chan);
                    break;
                // warn
                case 1:
                    context = string.Format(c_Context, HTMLString.String(in chan, StringColor.maroon));
                    break;
                // err
                case 2:
                    context = string.Format(c_Context, HTMLString.String(in chan, StringColor.teal));
                    break;
            }

            return c_MessagePrefix + string.Format(c_Context, channel.ToString()) + c_WhiteSpace + msg;
        }

        /// <summary>
        /// 해당 채널(<paramref name="channel"/>) 에 <paramref name="msg"/> 로그를 보냅니다.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="msg"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(LogChannel channel, in string msg) => Log(channel, in msg, null);
        /// <summary><inheritdoc cref="Log(LogChannel, in string)"/>></summary>
        /// <param name="channel"></param>
        /// <param name="msg"></param>
        /// <param name="context"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(LogChannel channel, in string msg, UnityEngine.Object context)
        {
            if ((LogChannel & channel) != channel) return;

            string str = LogStringFormat(channel, in msg, 0);
            UnityEngine.Debug.Log(str, context);
#if UNITY_EDITOR
            s_EditorLogs += s_EditorLogs.IsNullOrEmpty() ? str : str.ReturnAtFirst();
#endif
        }

        /// <summary>
        /// 해당 채널(<paramref name="channel"/>) 에 <paramref name="msg"/> 주의 로그를 보냅니다.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="msg"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(LogChannel channel, in string msg) => LogWarning(channel, in msg, null);
        /// <summary><inheritdoc cref="LogWarning(LogChannel, in string)"/></summary>
        /// <param name="channel"></param>
        /// <param name="msg"></param>
        /// <param name="context"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(LogChannel channel, in string msg, UnityEngine.Object context)
        {
            if ((LogChannel & channel) != channel) return;

            string str = LogStringFormat(channel, in msg, 1);
            UnityEngine.Debug.LogWarning(str, context);
#if UNITY_EDITOR
            s_EditorLogs += s_EditorLogs.IsNullOrEmpty() ? str : str.ReturnAtFirst();
#endif
        }

        public static string LogErrorString(LogChannel channel, in string msg) => LogStringFormat(channel, in msg, 2);
        /// <summary>
        /// 해당 채널(<paramref name="channel"/>) 에 <paramref name="msg"/> 에러 로그를 보냅니다.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(LogChannel channel, in string msg) => LogError(channel, in msg, null);
        /// <summary><inheritdoc cref="LogError(LogChannel, in string)"/></summary>
        /// <param name="channel"></param>
        /// <param name="msg"></param>
        /// <param name="context"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(LogChannel channel, in string msg, UnityEngine.Object context)
        {
            string str = LogStringFormat(channel, in msg, 2);
            UnityEngine.Debug.LogError(str, context);
#if UNITY_EDITOR
            s_EditorLogs += s_EditorLogs.IsNullOrEmpty() ? str : str.ReturnAtFirst();
#endif
        }

        /// <summary><inheritdoc cref="Log(LogChannel, in string)"/>></summary>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ToLog(this string msg, LogChannel channel = LogChannel.Debug)
        {
            if ((LogChannel & channel) != channel) return;

            Log(channel, in msg);
        }
        /// <summary><inheritdoc cref="LogWarning(LogChannel, in string)"/></summary>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void ToLogError(this string msg, LogChannel channel = LogChannel.Debug)
        {
            LogError(channel, in msg);
        }

#line default

        #endregion

        #region Threading

        /// <summary>
        /// 이 메소드가 실행된 <seealso cref="System.Threading.Thread"/> 가 Unity 의 메인 스크립트 스레드인지 반환합니다.
        /// </summary>
        /// <returns></returns>
#if UNITYENGINE && UNITY_COLLECTIONS
        [Unity.Collections.NotBurstCompatible]
#endif
        public static bool AssertIsMainThread()
        {
            Threading.ThreadInfo currentThread = Threading.ThreadInfo.CurrentThread;

            return CoreApplication.Instance.MainThread.Equals(currentThread);
        }
        /// <summary><inheritdoc cref="AssertIsMainThread"/></summary>
        /// <remarks>
        /// 만약 메인 스레드가 아닐 경우 에러 로그를 발생시킵니다.
        /// </remarks>
#if UNITYENGINE && UNITY_COLLECTIONS
        [Unity.Collections.NotBurstCompatible]
#endif
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void AssertMainThread()
        {
#if UNITY_EDITOR
            if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                LogError(LogChannel.Core,
                    $"Thread affinity error. Expected thread(MAINTHREAD) but {Threading.ThreadInfo.CurrentThread}");
            }
            return;
#endif
            AssertThreadAffinity(CoreApplication.Instance.MainThread);
        }
        /// <summary>
        /// 해당 스레드의 정보를 통해, 이 메소드가 실행된 <seealso cref="System.Threading.Thread"/> 가 일치하는지 반환합니다.
        /// </summary>
        /// <remarks>
        /// 만약 일치하지 않는다면 에러 로그를 발생시킵니다. 
        /// <br/><br/>
        /// C# 에서는 스레드의 선호도를 직접적으로 가져올 수 있는 방법이 제한적입니다. 
        /// <see cref="System.Diagnostics.Process"/> 를 통하여 선호도를 확인 할 수 있지만, 이는 Win32.dll 등 과 같은 
        /// 현재 프로그램이 실행되는 Operating 시스템에 크게 영향을 받기 때문에 적합하지 않습니다.
        /// <see cref="System.Threading.Thread.ManagedThreadId"/> 는 <see cref="GC"/> 에서 관리되는 Low-Level 관리 인덱스이며, 
        /// 이는 스레드 선호도를 의미하지 않습니다. 해당 인덱스를 통해 다른 스레드임을 확인하고, 만약 다른 스레드라면 다른 선호도를 갖고있다고 판단합니다.
        /// <br/><br/>
        /// 다른 인덱스이어도 같은 스레드 선호도를 공유하는 예외사항이 있습니다. (ex. id = 1(Affinity 0), id = 8(Affinity 0)) 
        /// </remarks>
        /// <param name="expectedAffinity"></param>
#if UNITYENGINE && UNITY_COLLECTIONS
        [Unity.Collections.NotBurstCompatible]
#endif
        [System.Diagnostics.Conditional("DEBUG_MODE")]
        public static void AssertThreadAffinity(in Threading.ThreadInfo expectedAffinity)
        {
            Threading.ThreadInfo currentThread = Threading.ThreadInfo.CurrentThread;

            if (expectedAffinity.Equals(currentThread)) return;

            //throw new InvalidOperationException($"Thread affinity error. Expected thread({expectedAffinity}) but {currentThread}");
            LogError(LogChannel.Core,
                $"Thread affinity error. Expected thread({expectedAffinity}) but {currentThread}");
        }
        /// <summary>
        /// 이 스레드가 <paramref name="other"/> 의 스레드와 같은 스레드인지 확인합니다. 
        /// 만약 다르다면 로그 에러를 표시합니다.
        /// </summary>
        /// <param name="other"></param>
        public static void Validate(this in Threading.ThreadInfo other)
        {
            AssertThreadAffinity(in other);
        }

        #endregion
    }
}
