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
