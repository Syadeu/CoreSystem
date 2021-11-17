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

using Syadeu.Collections;
using System;

namespace Syadeu.Presentation
{
    public struct PresentationResult
    {
        public static PresentationResult Normal = new PresentationResult(ResultFlag.Normal, string.Empty);

        internal ResultFlag m_Result;
        internal Exception m_Exception;
        internal string m_Message;

        private PresentationResult(ResultFlag flag, string msg)
        {
            m_Result = flag;
            m_Exception = null;
            m_Message = msg;
        }
        private PresentationResult(Exception ex, ResultFlag flag = ResultFlag.Error)
        {
            m_Result = flag;
            m_Exception = ex;
            m_Message = string.Empty;
        }

        public static PresentationResult Warning(string msg) => new PresentationResult(ResultFlag.Warning, msg);
        public static PresentationResult Error(string msg) => new PresentationResult(ResultFlag.Error, msg);
        public static PresentationResult Error(Exception ex) => new PresentationResult(ex);
    }
}
