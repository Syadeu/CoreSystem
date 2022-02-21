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

using Syadeu.Collections.Buffer.LowLevel;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace Syadeu.Collections.Reflection
{
    [BurstCompatible]
    public struct DelegateWrapper
    {
        private readonly TypeInfo m_Type;
        private readonly System.Reflection.BindingFlags m_BindingFlags;
        private readonly FixedString128Bytes m_MethodName;
        private readonly FixedList512Bytes<TypeInfo> m_ArgumentTypes;

        [NotBurstCompatible]
        public DelegateWrapper(Delegate action)
        {
            var parameters = action.Method.GetParameters();

            m_Type = action.Method.DeclaringType.ToTypeInfo();
            m_BindingFlags = System.Reflection.BindingFlags.Static;
            {
                if (action.Method.IsPublic) m_BindingFlags = System.Reflection.BindingFlags.Public;
                else m_BindingFlags = System.Reflection.BindingFlags.NonPublic;
            }
            m_MethodName = action.Method.Name;

            m_ArgumentTypes = new FixedList512Bytes<TypeInfo>();
            for (int i = 0; i < parameters.Length; i++)
            {
                m_ArgumentTypes[i] = parameters[i].ParameterType.ToTypeInfo();
            }
        }

        [NotBurstCompatible]
        private Type[] GetArgumentTypes()
        {
            if (m_ArgumentTypes.Length == 0) return null;

            Type[] types = new Type[m_ArgumentTypes.Length];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = m_ArgumentTypes[i].Type;
            }
            return types;
        }

        [NotBurstCompatible]
        public object[] ConvertToArguments(UnsafeReference<byte> bytes, int length)
        {
#if DEBUG_MODE
            if (m_ArgumentTypes.Length != length)
            {
                throw new InvalidOperationException();
            }
#endif
            UnsafeReference<byte> currentIndex = bytes;
            object[] args = new object[m_ArgumentTypes.Length];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] =
                    Marshal.PtrToStructure(currentIndex.IntPtr, m_ArgumentTypes[i].Type);

                currentIndex += m_ArgumentTypes[i].Size;
            }

            return args;
        }

        [NotBurstCompatible]
        public object DynamicInvoke(object obj, params object[] args)
        {
            System.Reflection.MethodInfo methodInfo =
                m_Type.Type.GetMethod(
                    m_MethodName.ToString(), m_BindingFlags, null, GetArgumentTypes(), null);
#if DEBUG_MODE
            if (methodInfo == null)
            {
                throw new InvalidOperationException();
            }
#endif
            return methodInfo.Invoke(obj, args);
        }
    }
}
