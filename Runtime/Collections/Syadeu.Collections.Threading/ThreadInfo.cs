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
using System.Threading;
using Unity.Collections;

namespace Syadeu.Collections.Threading
{
    /// <summary>
    /// Thread 관련 각종 작업을 할 수 있는 구조체입니다.
    /// </summary>
    [BurstCompatible]
    public struct ThreadInfo : IEquatable<ThreadInfo>, IEquatable<Thread>
    {
        /// <summary>
        /// 현재 스레드 정보를 가져옵니다.
        /// </summary>
        public static ThreadInfo CurrentThread => new ThreadInfo(Thread.CurrentThread);

        //
        //
        // https://www.sysnet.pe.kr/2/0/492

        private readonly int m_ManagedThreadID;
        private readonly int m_HashCode;
        private readonly FixedString512Bytes m_Name;

        public string Name => m_Name.ToString();

        [NotBurstCompatible]
        public ThreadInfo(Thread thread)
        {
            m_ManagedThreadID = thread.ManagedThreadId;
            m_HashCode = thread.GetHashCode();

            if (string.IsNullOrEmpty(thread.Name))
            {
                m_Name = "None";
            }
            else m_Name = thread.Name;
        }

        [NotBurstCompatible]
        public void ValidateAndThrow()
        {
            Thread currentThread = Thread.CurrentThread;

            if (!Equals(currentThread))
            {
                UnityEngine.Debug.LogError(
                    $"Thread affinity error. Expected thread({this}) but {currentThread}");
            }
        }
        [NotBurstCompatible]
        public bool Validate()
        {
            Thread currentThread = Thread.CurrentThread;

            return Equals(currentThread);
        }

        public bool Equals(ThreadInfo other)
        {
            if (m_ManagedThreadID != other.m_ManagedThreadID) return false;
            // 아마도 같은 Native thread (Processor) 로 매핑된 새로운 다른 스레드 객체일 것으로 추측됨.
            // 같은 native 로 연결되었으면 같은 스레드라고 판단하는 것으로
            //else if (m_HashCode != other.m_HashCode)
            //{
            //}

            return true;
        }
        [NotBurstCompatible]
        public bool Equals(Thread other)
        {
            if (m_ManagedThreadID != other.ManagedThreadId) return false;
            // 아마도 같은 Native thread (Processor) 로 매핑된 새로운 다른 스레드 객체일 것으로 추측됨.
            // 같은 native 로 연결되었으면 같은 스레드라고 판단하는 것으로
            //else if (m_HashCode != other.GetHashCode())
            //{
            //}

            return true;
        }
        [NotBurstCompatible]
        public override string ToString()
        {
            string name = m_Name.ToString();
            return $"Thread({name}, {m_ManagedThreadID}, {m_HashCode})";
        }
    }
}
