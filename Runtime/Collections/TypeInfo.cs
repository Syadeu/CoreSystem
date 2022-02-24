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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections
{
    /// <summary>
    /// Runtime 중 기본 <see cref="System.Type"/> 의 정보를 저장하고, 해당 타입의 binary 크기, alignment를 저장합니다.
    /// </summary>
    /// <remarks>
    /// 24 bytes
    /// </remarks>
    [BurstCompatible]
    public readonly struct TypeInfo : IValidation, IEquatable<TypeInfo>
    {
        private readonly FixedString512Bytes m_TypeHandle;
        private readonly int m_Size;
        private readonly int m_Align;

        private readonly int m_HashCode;

        [NotBurstCompatible]
        public Type Type
        {
            get
            {
                if (m_TypeHandle.Value.Equals(IntPtr.Zero))
                {
                    return null;
                }
                return Type.GetType(m_TypeHandle.ToString());
            }
        }
        public int Size => m_Size;
        public int Align => m_Align;

        internal TypeInfo(Type type, int size, int align, int hashCode)
        {
            m_TypeHandle = type.AssemblyQualifiedName;
            m_Size = size;
            m_Align = align;

            unchecked
            {
                // https://stackoverflow.com/questions/102742/why-is-397-used-for-resharper-gethashcode-override
                m_HashCode = hashCode * 397;
            }
        }

        [NotBurstCompatible]
        public static TypeInfo GetTypeInfo(Type type) => CollectionUtility.GetTypeInfo(type);
        [NotBurstCompatible]
        public static TypeInfo GetTypeInfo<T>() => CollectionUtility.GetTypeInfo(TypeHelper.TypeOf<T>.Type);

        public override int GetHashCode() => m_HashCode;

        public bool Equals(TypeInfo other) => m_TypeHandle.Equals(other.m_TypeHandle);

        public bool IsValid()
        {
            if (m_TypeHandle.IsEmpty ||
                m_Size == 0 || m_HashCode == 0) return false;

            return true;
        }
    }
}
