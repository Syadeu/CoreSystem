using System;

namespace Syadeu
{
    public sealed class CoreSystemException : Exception
    {
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
