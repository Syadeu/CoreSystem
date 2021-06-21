using MoonSharp.Interpreter;

namespace Syadeu.Database
{
    public abstract class LuaProxyEntity<T>
    {
        private readonly T m_Target;
        protected T Target => m_Target;

        [MoonSharpHidden]
        public LuaProxyEntity(T t)
        {
            m_Target = t;
        }
    }
}
