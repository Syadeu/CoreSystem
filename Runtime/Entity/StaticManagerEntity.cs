using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Syadeu.Entities
{
    public abstract class StaticManagerEntity : ManagerEntity
    {
        protected static CoreSystem System { get; set; }
        public SystemFlag Flag { get; protected set; }

        protected static readonly ConcurrentQueue<Action> m_EnforceOrder = new ConcurrentQueue<Action>();

        public static void ThreadAwaiter(int milliseconds)
        {
            #region Awaiter
#if WINDOWS_UWP
            Task.Delay(milliseconds).Wait();
#else
            Thread.Sleep(milliseconds);
#endif
            #endregion
        }

        public static void AwaitForNotNull<T>(ref T component, ref bool order, Action init)
        {
            if (!order)
            {
                //$"CoreSystem.ThreadSafe :: 백그라운드 스레드에서 Mono 매니저 객체({typeof(T).Name})를 생성 요청함".ToLog();
                m_EnforceOrder.Enqueue(init);
                order = true;
            }

            do
            {
                ThreadAwaiter(10);
            } while (component == null);

            //$"CoreSystem.ThreadSafe :: 매니저 객체({typeof(T).Name})가 정상적으로 생성되어 백그라운드 스레드에 반환됨".ToLog();
        }
    }
}
