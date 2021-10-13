﻿using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Syadeu.Collections
{
    /// <summary>
    /// Runtime 중 기본 <see cref="System.Type"/> 의 정보를 저장하고, 해당 타입의 binary 크기, alignment를 저장합니다.
    /// </summary>
    public readonly struct TypeInfo
    {
        private readonly RuntimeTypeHandle m_TypeHandle;
        private readonly int m_TypeIndex;
        private readonly int m_Size;
        private readonly int m_Align;

        private readonly int m_HashCode;

        public Type Type => Type.GetTypeFromHandle(m_TypeHandle);
        public int Index => m_TypeIndex;
        public int Size => m_Size;
        public int Align => m_Align;

        private TypeInfo(Type type, int index, int size, int align, int hashCode)
        {
            m_TypeHandle = type.TypeHandle;
            m_TypeIndex = index;
            m_Size = size;
            m_Align = align;

            unchecked
            {
                // https://stackoverflow.com/questions/102742/why-is-397-used-for-resharper-gethashcode-override
                m_HashCode = m_TypeIndex * 397 ^ hashCode;
            }
        }

        public static TypeInfo Construct(Type type, int index, int size, int align, int hashCode)
        {
            return new TypeInfo(type, index, size, align, hashCode);
        }
        public static TypeInfo Construct(Type type, int index, int hashCode)
        {
            if (!UnsafeUtility.IsUnmanaged(type))
            {
                Debug.LogError(
                    $"Could not resovle type of {TypeHelper.ToString(type)} is not ValueType.");

                return new TypeInfo(type, index, 0, 0, hashCode);
            }

            return new TypeInfo(type, index, UnsafeUtility.SizeOf(type), TypeHelper.AlignOf(type), hashCode);
        }

        public override int GetHashCode() => m_HashCode;
    }
}