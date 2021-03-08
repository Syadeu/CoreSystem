using Syadeu.Mono;
using System;
using UnityEngine.Diagnostics;

namespace Syadeu
{
    public sealed class CoreSystemException : Exception
    {
        public static string TextBuilder(CoreSystemExceptionFlag from, string msg, Exception ex = null)
        {
            if (ex == null)
            {
                return $"CoreSystem.{from} :: {msg}";
            }
            return $"CoreSystem.{from} :: {msg}\n호출지점: {ex.StackTrace}";
        }
        public static void SendCrash(CoreSystemExceptionFlag from, string msg, Exception ex)
        {
            UnityEngine.Debug.LogError(TextBuilder(from, msg, ex));
#if !UNITY_EDITOR
            if (SyadeuSettings.Instance.m_CrashAfterException)
            {
                Utils.ForceCrash(ForcedCrashCategory.FatalError);
            }
#endif
        }

        public CoreSystemException(CoreSystemExceptionFlag from, string msg)
            : base($"CoreSystem.{from} :: {msg}")
        {
        }
        public CoreSystemException(CoreSystemExceptionFlag from, string msg, Exception inner) 
            : base($"CoreSystem.{from} :: {msg}", 
                  inner)
        {
        }
        public CoreSystemException(CoreSystemExceptionFlag from, string msg, string customStackTrace) 
            : base($"CoreSystem.{from} :: {msg}\n호출지점: {customStackTrace}")
        {
        }
        public CoreSystemException(CoreSystemExceptionFlag from, string msg, string customStackTrace, Exception inner) 
            : base($"CoreSystem.{from} :: {msg}\n호출지점: {customStackTrace}\n", inner)
        {
        }
    }
}
