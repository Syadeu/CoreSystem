// Copyright 2022 Seung Ha Kim
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

using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections
{
    [BurstCompatible]
    public struct FixedConstAction : IConstActionReference
    {
        private Guid m_Guid;
        private FixedList4096Bytes<byte> m_Arguments;

        private unsafe UnsafeReference<byte> ArgumentPtr
        {
            get
            {
                byte* ptr = UnsafeBufferUtility.AsBytes(ref m_Arguments, out _);

                return ptr;
            }
        }

        public Guid Guid => m_Guid;
        [NotBurstCompatible]
        public object[] Arguments
        {
            get
            {
                if (IsEmpty() || 
                    !ConstActionUtilities.TryGetWithGuid(m_Guid, out ConstActionUtilities.Info info))
                {
                    return Array.Empty<object>();
                }

                int index = 0;
                object[] args = new object[info.ArgumentFields.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    var ptr = ArgumentPtr + index;
                    args[i] = Marshal.PtrToStructure(ptr.IntPtr, info.Type);

                    index += UnsafeUtility.SizeOf(info.Type);
                }

                return args;
            }
        }

        [NotBurstCompatible]
        internal unsafe FixedConstAction(ConstActionReference t)
        {
            this = default(FixedConstAction);

            m_Guid = t.Guid;
            ConstActionUtilities.TryGetWithGuid(t.Guid, out ConstActionUtilities.Info info);

            int size;
            for (int i = 0; i < t.Arguments.Length; i++)
            {
                object arg = t.Arguments[i];
                if (info.Type.IsValueType)
                {
                    size = UnsafeUtility.SizeOf(info.Type);
                    UnsafeBufferUtility.GetBytes(arg, ref m_Arguments);
                }
                else if (info.Type.Equals(TypeHelper.TypeOf<string>.Type))
                {
                    size = 512;

                    FixedString512Bytes str = (string)arg;
                    byte* bytes = str.GetUnsafePtr();
                    for (int x = 0; x < 512; x++)
                    {
                        m_Arguments.Add(bytes[x]);
                    }
                }
                else
                {
                    throw new Exception("??");
                }
            }
        }
        [NotBurstCompatible]
        public FixedConstAction(Type constActionType)
        {
            this = default(FixedConstAction);
            if (!ConstActionUtilities.HashMap.TryGetValue(constActionType, out var info))
            {
                return;
            }

            m_Guid = info.Guid;
        }

        public bool IsEmpty() => Guid.Equals(Guid.Empty);
        public void SetArguments(params object[] args)
        {
            throw new NotImplementedException();
        }
    }
}
