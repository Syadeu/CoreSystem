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
    }
}
