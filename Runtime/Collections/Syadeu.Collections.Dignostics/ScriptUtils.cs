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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System.Diagnostics;

namespace Syadeu.Collections.Dignostics
{
    public static class ScriptUtils
    {
        public static string GetCallerScriptPath()
        {
            return new StackTrace(true).GetFrame(1).GetFileName();
        }
        public static StackFrame GetCallerFrame()
        {
            return new StackTrace(true).GetFrame(1);
        }
        public static StackFrame GetCallerFrame(int depth)
        {
            return new StackTrace(true).GetFrame(1 + depth);
        }

        public static string ToStringFormat(StackFrame stackFrame)
        {
            var method = stackFrame.GetMethod();

            string methodName = method.Name;
            string className = method.DeclaringType.Name;

            const string c_Format = "{0}.{1} (at {2}:{3})";
            return string.Format(c_Format, className, methodName, stackFrame.GetFileName(), stackFrame.GetFileLineNumber());
        }
    }
}
