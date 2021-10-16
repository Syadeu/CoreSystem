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

        private bool m_MarkerSet = false;
        private Unity.Profiling.ProfilerMarker m_Marker;

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
            m_MarkerSet = false;
            s_Container.Enqueue(this);
        }

        public void SetProfiler(string name)
        {
            m_MarkerSet = true;
            m_Marker = new Unity.Profiling.ProfilerMarker(name);
        }
        public void SetAction(Action action)
        {
            m_Action = action;
        }
        public void Invoke()
        {
            if (m_MarkerSet) m_Marker.Begin();
            m_Action?.Invoke();
            if (m_MarkerSet) m_Marker.End();
        }
    }
    public sealed class ActionWrapper<T>
    {
        private static readonly CLRContainer<ActionWrapper<T>> s_Container;
        private Action<T> m_Action;

        private bool m_MarkerSet = false;
        private Unity.Profiling.ProfilerMarker m_Marker;

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
            m_MarkerSet = false;
            s_Container.Enqueue(this);
        }

        public void SetProfiler(string name)
        {
            m_MarkerSet = true;
            m_Marker = new Unity.Profiling.ProfilerMarker(name);
        }
        public void SetAction(Action<T> action)
        {
            m_Action = action;
        }
        public void Invoke(T t)
        {
            if (m_MarkerSet) m_Marker.Begin();
            m_Action?.Invoke(t);
            if (m_MarkerSet) m_Marker.End();
        }
    }
}
