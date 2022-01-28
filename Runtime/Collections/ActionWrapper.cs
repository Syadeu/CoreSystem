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

using Syadeu.Collections.Buffer;
using System;

namespace Syadeu.Collections
{
    public sealed class ActionWrapper : IActionWrapper
    {
        private static readonly ObjectPool<ActionWrapper> s_Container;
        public Action Action;

        private bool m_MarkerSet = false;
        private Unity.Profiling.ProfilerMarker m_Marker;

        static ActionWrapper()
        {
            s_Container = new ObjectPool<ActionWrapper>(Factory, null, null, null);
        }
        private static ActionWrapper Factory()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new ActionWrapper();
#pragma warning restore CS0618 // Type or member is obsolete
        }
        /// <summary>
        /// <see cref="GetWrapper"/> 를 사용하세요.
        /// </summary>
        [Obsolete("Use ActionWrapper.GetWrapper")]
        public ActionWrapper() { }
        public static ActionWrapper GetWrapper() => s_Container.Get();
        public void Reserve()
        {
            Action = null;
            m_MarkerSet = false;
            s_Container.Reserve(this);
        }

        public void SetProfiler(string name)
        {
            m_MarkerSet = true;
            m_Marker = new Unity.Profiling.ProfilerMarker(name);
        }
        public void SetAction(Action action)
        {
            Action = action;
        }
        public void Invoke()
        {
            if (m_MarkerSet) m_Marker.Begin();
            Action?.Invoke();
            if (m_MarkerSet) m_Marker.End();
        }

        void IActionWrapper.Invoke(params object[] args) => Invoke();
    }
    public interface IActionWrapper
    {
        void Reserve();
        void Invoke(params object[] args);
    }
    public sealed class ActionWrapper<T> : IActionWrapper
    {
        private static readonly ObjectPool<ActionWrapper<T>> s_Container;
        public Action<T> Action;

        private bool m_MarkerSet = false;
        private Unity.Profiling.ProfilerMarker m_Marker;

        static ActionWrapper()
        {
            s_Container = new ObjectPool<ActionWrapper<T>>(Factory, null, null, null);
        }
        private static ActionWrapper<T> Factory()
        {
            return new ActionWrapper<T>();
        }
        /// <summary>
        /// <see cref="GetWrapper"/> 를 사용하세요.
        /// </summary>
        public ActionWrapper() { }
        public static ActionWrapper<T> GetWrapper() => s_Container.Get();
        public void Reserve()
        {
            Action = null;
            m_MarkerSet = false;
            s_Container.Reserve(this);
        }

        public void SetProfiler(string name)
        {
            m_MarkerSet = true;
            m_Marker = new Unity.Profiling.ProfilerMarker(name);
        }
        public void SetAction(Action<T> action)
        {
            Action = action;
        }
        public void Invoke(T t)
        {
            if (m_MarkerSet) m_Marker.Begin();
            try
            {
                Action?.Invoke(t);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            if (m_MarkerSet) m_Marker.End();
        }

        void IActionWrapper.Invoke(params object[] args) => Invoke((T)args[0]);
    }
}
