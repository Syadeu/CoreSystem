using MoonSharp.Interpreter;

namespace Syadeu.Collections.Lua
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
