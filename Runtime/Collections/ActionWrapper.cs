#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;
using System.Diagnostics;

namespace Syadeu.Collections
{
    public sealed class ActionWrapper
    {
        private static readonly CLRContainer<ActionWrapper> s_Container;
        private Action m_Action;

#if DEBUG_MODE
        private bool m_MarkerSet = false;
        private Unity.Profiling.ProfilerMarker m_Marker;
#endif

        static ActionWrapper()
        {
            s_Container = new CLRContainer<ActionWrapper>(Factory);
        }
        private static ActionWrapper Factory()
        {
            return new ActionWrapper();
        }
        private ActionWrapper()
        {
            
        }
        public static ActionWrapper GetWrapper() => s_Container.Dequeue();
        public void Reserve()
        {
            m_Action = null;
#if DEBUG_MODE
            m_MarkerSet = false;
#endif
            s_Container.Enqueue(this);
        }

        [Conditional("DEBUG_MODE")]
        public void SetProfiler(string name)
        {
#if DEBUG_MODE
            m_MarkerSet = true;
            m_Marker = new Unity.Profiling.ProfilerMarker(name);
#endif
        }
        public void SetAction(Action action)
        {
            m_Action = action;
        }
        public void Invoke()
        {
#if DEBUG_MODE
            if (m_MarkerSet) m_Marker.Begin();
#endif
            m_Action?.Invoke();
#if DEBUG_MODE
            if (m_MarkerSet) m_Marker.End();
#endif
        }
    }
    public sealed class ActionWrapper<T>
    {
        private static readonly CLRContainer<ActionWrapper<T>> s_Container;
        private Action<T> m_Action;

#if DEBUG_MODE
        private bool m_MarkerSet = false;
        private Unity.Profiling.ProfilerMarker m_Marker;
#endif

        static ActionWrapper()
        {
            s_Container = new CLRContainer<ActionWrapper<T>>(Factory);
        }
        private static ActionWrapper<T> Factory()
        {
            return new ActionWrapper<T>();
        }
        private ActionWrapper()
        {

        }
        public static ActionWrapper<T> GetWrapper() => s_Container.Dequeue();
        public void Reserve()
        {
            m_Action = null;
#if DEBUG_MODE
            m_MarkerSet = false;
#endif
            s_Container.Enqueue(this);
        }

        [Conditional("DEBUG_MODE")]
        public void SetProfiler(string name)
        {
#if DEBUG_MODE
            m_MarkerSet = true;
            m_Marker = new Unity.Profiling.ProfilerMarker(name);
#endif
        }
        public void SetAction(Action<T> action)
        {
            m_Action = action;
        }
        public void Invoke(T t)
        {
#if DEBUG_MODE
            if (m_MarkerSet) m_Marker.Begin();
#endif
            m_Action?.Invoke(t);
#if DEBUG_MODE
            if (m_MarkerSet) m_Marker.End();
#endif
        }
    }
}
