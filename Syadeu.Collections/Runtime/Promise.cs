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

using System;

namespace Syadeu.Collections
{
    public class Promise : IPromise, IDisposable
    {
        private object m_Value;
        private Action<object> m_OnCompleted;

        public bool HasValue => m_Value != null;
        public object Value => m_Value;

        /// <summary>
        /// 작업이 완료되고 값이 할당될 때 실행되는 이벤트입니다.
        /// </summary>
        /// <remarks>
        /// 이미 작업이 완료되었거나 값을 할당받은 경우, 추가된 이벤트는 즉시 실행됩니다.
        /// </remarks>
        public event Action<object> OnCompleted
        {
            add
            {
                if (m_Value != null)
                {
                    value?.Invoke(m_Value);
                    return;
                }

                m_OnCompleted += value;
            }
            remove
            {
                m_OnCompleted -= value;
            }
        }

        public Promise(object value)
        {
            m_Value = value;
        }
        public Promise(IPromiseProvider provider)
        {
            provider.OnComplete(OnCompleteMethod);
        }
        ~Promise()
        {
            Dispose();
        }
        public void Dispose()
        {
            m_Value = null;
            m_OnCompleted = null;
        }

        protected virtual void OnCompleteMethod(object obj)
        {
            m_Value = obj;

            m_OnCompleted?.Invoke(obj);
            m_OnCompleted = null;
        }
    }
    public class Promise<T> : IPromise, IDisposable
    {
        public static Promise<T> Empty { get; } = new Promise<T>();

        private T m_Value;
        private bool m_IsCompleted;
        private Action<T> m_OnCompleted;

        public bool HasValue => m_IsCompleted;
        /// <inheritdoc cref="IPromise.Value"/>
        public T Value => m_Value;
        object IPromise.Value => m_Value;

        /// <inheritdoc cref="Promise.OnCompleted"/>
        public event Action<T> OnCompleted
        {
            add
            {
                if (m_IsCompleted)
                {
                    value?.Invoke(m_Value);
                    return;
                }

                m_OnCompleted += value;
            }
            remove
            {
                m_OnCompleted -= value;
            }
        }

        public Promise() { }
        public Promise(T obj)
        {
            m_Value = obj;
            m_IsCompleted = true;
        }
        public Promise(IPromiseProvider provider)
        {
            provider.OnComplete(OnCompleteMethod);

            m_IsCompleted = false;
        }
        public Promise(IPromiseProvider<T> provider)
        {
            provider.OnComplete(OnCompleteMethod);

            m_IsCompleted = false;
        }
        ~Promise()
        {
            Dispose();
        }
        public void Dispose()
        {
            OnDispose();

            m_Value = default(T);
            m_IsCompleted = false;
            m_OnCompleted = null;
        }

        private void OnCompleteMethod(object obj) => OnCompleteMethod((T)obj);
        protected virtual void OnCompleteMethod(T obj)
        {
            m_Value = (T)obj;

            m_OnCompleted?.Invoke((T)obj);
            m_IsCompleted = true;

            m_OnCompleted = null;
        }
        protected virtual void OnDispose() { }

        public static implicit operator Promise<T>(T t)
        {
            return new Promise<T>(t);
        }
        public static explicit operator T(Promise<T> t) => t.Value;
    }

    internal interface IPromise
    {
        /// <summary>
        /// 값을 가지고 있나요?
        /// </summary>
        bool HasValue { get; }
        /// <summary>
        /// 현재 가지고 있는 값
        /// </summary>
        object Value { get; }
    }

    public interface IPromiseProvider
    {
        void OnComplete(Action<object> obj);
    }
    public interface IPromiseProvider<T>
    {
        void OnComplete(Action<T> obj);
    }
}
