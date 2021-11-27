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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;

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
        /// <summary>
        /// <see cref="GetWrapper"/> 를 사용하세요.
        /// </summary>
        public ActionWrapper() { }
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
        /// <summary>
        /// <see cref="GetWrapper"/> 를 사용하세요.
        /// </summary>
        public ActionWrapper() { }
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
            try
            {
                m_Action?.Invoke(t);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            if (m_MarkerSet) m_Marker.End();
        }
    }
}
