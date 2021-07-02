//#undef UNITY_ADDRESSABLES


#if UNITY_EDITOR
#endif

#if UNITY_ADDRESSABLES
#endif

using Syadeu.Database;
using System;

namespace Syadeu.Presentation
{
    public struct PresentationResult
    {
        public static PresentationResult Normal = new PresentationResult(ResultFlag.Normal, string.Empty);

        internal ResultFlag m_Result;
        internal Exception m_Exception;
        internal string m_Message;

        public PresentationResult(ResultFlag flag, string msg)
        {
            m_Result = flag;
            m_Exception = null;
            m_Message = msg;
        }
        public PresentationResult(Exception ex, ResultFlag flag = ResultFlag.Error)
        {
            m_Result = flag;
            m_Exception = ex;
            m_Message = string.Empty;
        }
    }
}
