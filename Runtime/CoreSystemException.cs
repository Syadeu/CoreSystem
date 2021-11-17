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
            return $"CoreSystem.{from} :: {msg}\n{ex.Message}\n호출지점: {ex.StackTrace}";
        }
        public static void SendCrash(CoreSystemExceptionFlag from, string msg, Exception ex)
        {
            UnityEngine.Debug.LogError(TextBuilder(from, msg, ex));
#if !UNITY_EDITOR
            if (CoreSystemSettings.Instance.m_CrashAfterException)
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
    public sealed class CoreSystemThreadSafeMethodException : Exception
    {
        public CoreSystemThreadSafeMethodException(string methodName)
            : base($"CoreSystem.{CoreSystemExceptionFlag.Background} :: 메소드 {methodName} 는 외부 스레드에서 실행될 수 없습니다.")
        {
        }
    }
}
