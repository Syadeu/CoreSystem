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

using Syadeu.Collections;
using Syadeu.Internal;
using System;
using System.Reflection;

namespace Syadeu.Mono.Console
{
    public delegate bool CommandRequiresDelegate();

    public class CommandRequires
    {
        internal Type m_ReturnType;
        internal MethodInfo m_Method;

        internal object m_ExpectValue;
        internal CommandRequiresDelegate m_ExpectDelegate = null;

        internal CommandRequires(MethodInfo method, object expectValue)
        {
            if (method.ReturnType == null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                    "콘솔 명령어의 반환값은 void (null) 타입이 될 수 없습니다.");
            }
            if (!method.IsStatic)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                    @"콘솔 명령어의 필요값을 받는 메소드가 Static이 아닙니다.
                    메소드는 Static 메소드만 가능합니다.");
            }

            Type expectType = expectValue.GetType();
            if (!method.ReturnType.Equals(expectType))
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                    "콘솔 명령어의 필요값을 받는 메소드 리턴타입과 일치값(expectValue)의 타입이 일치하지 않습니다.");
            }

            m_ReturnType = method.ReturnType;
            m_Method = method;

            m_ExpectValue = expectValue;
        }
        internal CommandRequires(MethodInfo method, CommandRequiresDelegate @delegate)
        {
            if (method.ReturnType == null)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                    "콘솔 명령어의 반환값은 void (null) 타입이 될 수 없습니다.");
            }
            if (method.ReturnType != TypeHelper.TypeOf<bool>.Type)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                    "delegate를 수행하는 콘솔 명령어의 메소드는 리턴이 boolean 타입이어야됩니다.");
            }
            if (!method.IsStatic)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                    @"콘솔 명령어의 필요값을 받는 메소드가 Static이 아닙니다.
                    메소드는 Static 메소드만 가능합니다.");
            }

            m_ReturnType = TypeHelper.TypeOf<bool>.Type;
            m_Method = method;

            m_ExpectDelegate = @delegate;
        }

        private bool InvokeBoolean()
        {
            object output = m_Method.Invoke(null, null);
            return Convert.ToBoolean(output);
        }
        private int InvokeInteger()
        {
            object output = m_Method.Invoke(null, null);
            return Convert.ToInt32(output);
        }
        private float InvokeSingle()
        {
            object output = m_Method.Invoke(null, null);
            return Convert.ToSingle(output);
        }

        internal bool Invoke()
        {
            object output;
            if (m_ReturnType == TypeHelper.TypeOf<bool>.Type)
            {
                output = InvokeBoolean();
            }
            else if (m_ReturnType == TypeHelper.TypeOf<int>.Type)
            {
                output = InvokeInteger();
            }
            else if (m_ReturnType == TypeHelper.TypeOf<float>.Type)
            {
                output = InvokeSingle();
            }
            else
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                    $"콘솔 명령어 Requires에서 지원하지 않는 리턴타입({m_ReturnType.Name})을 반환했습니다.");
            }

            if (m_ExpectDelegate == null) return output.Equals(m_ExpectValue);
            else return output.Equals(m_ExpectDelegate.Invoke());
        }
    }
}
