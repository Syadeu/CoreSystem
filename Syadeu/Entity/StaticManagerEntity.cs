using Syadeu.Extentions.EditorUtils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Syadeu
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
            "EXCEPTION :: Thread safety has triggered".ToLog();
            if (!order)
            {
                m_EnforceOrder.Enqueue(init);
                order = true;
            }

            do
            {
                ThreadAwaiter(100);
            } while (component == null);

            "EXCEPTION :: Thread safety has completed".ToLog();
        }
    }
}
