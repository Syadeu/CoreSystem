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
