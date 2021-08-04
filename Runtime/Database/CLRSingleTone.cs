using System;

namespace Syadeu.Database
{
    public abstract class CLRSingleTone<T> : IDisposable where T : class, new()
    {
        private static T s_Instance;
        public static T Instance
        {
            get
            {
                if (s_Instance == null) s_Instance = new T();
                return s_Instance;
            }
        }
        ~CLRSingleTone()
        {
            Dispose();
        }

        public virtual void Dispose() { }
    }
}
