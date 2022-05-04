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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Syadeu.Collections.Buffer.LowLevel
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    public static unsafe class UnsafeBufferUtility
    {
        [BurstCompile]
        public static UnsafeReference<byte> AsBytes<T>(ref T t, out int length)
            where T : unmanaged
        {
            length = UnsafeUtility.SizeOf<T>();
            void* p = UnsafeUtility.AddressOf(ref t);
            
            return (byte*)p;
        }
        public static byte[] ToBytes(in UnsafeReference ptr, in int length)
        {
            if (!ptr.IsCreated || ptr.Ptr == null) return Array.Empty<byte>();

            byte[] arr = new byte[length];
            Marshal.Copy(ptr.IntPtr, arr, 0, length);

            return arr;
        }

        public static byte[] ObjectToByteArray(this object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public static object ByteArrayToObject(byte[] arrBytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }

        /// <summary>
        /// <see cref="FNV1a64"/> 알고리즘으로 바이너리 해시 연산을 하여 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        [BurstCompile]
        public static Hash Calculate<T>(this ref T t) where T : unmanaged
        {
            byte* bytes = AsBytes(ref t, out int length);
            Hash hash = new Hash(FNV1a64.Calculate(bytes, length));

            return hash;
        }

        [BurstCompile]
        public static bool BinaryComparer<T, U>(ref T x, ref U y)
            where T : unmanaged
            where U : unmanaged
        {
            byte*
                a = AsBytes(ref x, out int length),
                b = (byte*)UnsafeUtility.AddressOf(ref y);

            int index = 0;
            while (index < length && a[index].Equals(b[index]))
            {
                index++;
            }

            if (index != length) return false;
            return true;
        }
        public static int BinarySearch<T, U>(in UnsafeReference<T> buffer, in int length, T value, U comparer)
            where T : unmanaged
            where U : IComparer<T>
        {
            int index = NativeSortExtension.BinarySearch<T, U>(buffer, length, value, comparer);

            return index;
        }

        [BurstCompile]
        public static void Sort<T, U>(in UnsafeReference<T> buffer, in int length, U comparer)
            where T : unmanaged
            where U : unmanaged, IComparer<T>
        {
            for (int i = 0; i + 1 < length; i++)
            {
                int compare = comparer.Compare(buffer[i], buffer[i + 1]);
                if (compare > 0)
                {
                    Swap(buffer, i, i + 1);
                    Sort(buffer, in i, comparer);
                }
            }
        }

        [BurstCompile]
        public static void Swap<T>(in UnsafeReference<T> buffer, in int from, in int to)
            where T : unmanaged
        {
            T temp = buffer[from];
            buffer[from] = buffer[to];
            buffer[to] = temp;
        }

        public static bool Contains<T, U>(in UnsafeReference<T> buffer, in int length, U item) 
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            int index = IndexOf(buffer, length, item);

            return index >= 0;
        }
        public static bool ContainsRev<T, U>(in UnsafeReference<T> buffer, in int length, U item) 
            where T : unmanaged
            where U : unmanaged, IEquatable<T>
        {
            int index = IndexOfRev(buffer, length, item);

            return index >= 0;
        }

        [BurstCompile]
        public static int IndexOf<T, U>(in UnsafeReference<T> array, in int length, U item)
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            for (int i = 0; i < length; i++)
            {
                if (array[i].Equals(item)) return i;
            }
            return -1;
        }
        [BurstCompile]
        public static int IndexOfRev<T, U>(in UnsafeReference<T> array, in int length, U item)
            where T : unmanaged
            where U : unmanaged, IEquatable<T>
        {
            for (int i = 0; i < length; i++)
            {
                if (item.Equals(array[i])) return i;
            }
            return -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(this T[] array, T element)
            where T : IEquatable<T>
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(element)) return i;
            }
            return -1;
        }

        [BurstCompile]
        public static bool RemoveForSwapBack<T, U>(UnsafeReference<T> array, int length, U element)
            where T : unmanaged, IEquatable<U>
            where U : unmanaged
        {
            int index = IndexOf(array, length, element);
            if (index < 0) return false;

            for (int i = index + 1; i < length; i++)
            {
                array[i - 1] = array[i];
            }

            return true;
        }
        [BurstCompile]
        public static bool RemoveForSwapBackRev<T, U>(UnsafeReference<T> array, int length, U element)
            where T : unmanaged
            where U : unmanaged, IEquatable<T>
        {
            int index = IndexOfRev(in array, in length, element);
            if (index < 0) return false;

            for (int i = index + 1; i < length; i++)
            {
                array[i - 1] = array[i];
            }

            return true;
        }

        [BurstCompile]
        public static bool RemoveAtSwapBack<T>(UnsafeReference<T> array, int length, int index)
           where T : unmanaged
        {
            if (index < 0) return false;

            for (int i = index + 1; i < length; i++)
            {
                array[i - 1] = array[i];
            }

            return true;
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveForSwapBack<T>(this T[] array, T element)
            where T : IEquatable<T>
        {
            int index = array.IndexOf(element);
            if (index < 0) return false;

            for (int i = index + 1; i < array.Length; i++)
            {
                array[i - 1] = array[i];
            }

            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveAtSwapBack<T>(this T[] array, int index)
        {
            if (index < 0) return false;

            for (int i = index + 1; i < array.Length; i++)
            {
                array[i - 1] = array[i];
            }

            return true;
        }

        public static int GetBytes(object obj, ref FixedList128Bytes<byte> output)
        {
            UnsafeReference<byte> ptr;
            int length;

            if (obj is int integer)
            {
                int temp = integer;
                ptr = AsBytes(ref temp, out length);
            }
            else if (obj is bool boolean)
            {
                bool temp = boolean;
                ptr = AsBytes(ref temp, out length);
            }
            else if (obj is float single)
            {
                float temp = single;
                ptr = AsBytes(ref temp, out length);
            }
            else if (obj is double doub)
            {
                double temp = doub;
                ptr = AsBytes(ref temp, out length);
            }
            else
            {
                Debug.LogError("?? fatal error");
                return 0;
            }

            for (int i = 0; i < length; i++)
            {
                output.Add(ptr[i]);
            }

            return length;
        }

        #region Memory

        [BurstCompile]
        public static long CalculateFreeSpaceBetween(in UnsafeReference from, in int length, in UnsafeReference to)
        {
            UnsafeReference<byte> p = (UnsafeReference<byte>)from;

            return to - (p + length);
        }

        public static T DeepClone<T>(this T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Position = 0;

                return (T)formatter.Deserialize(stream);
            }
        }

        #endregion

        #region Native Array

        public static ref T ElementAtAsRef<T>(this NativeArray<T> t, in int index)
            where T : unmanaged
        {
            unsafe
            {
                void* buffer = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(t);
                return ref UnsafeUtility.ArrayElementAsRef<T>(buffer, index);
            }
        }

        #endregion

        #region Safety Checks

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly Dictionary<IntPtr, (DisposeSentinel, Allocator)> m_Safety
            = new Dictionary<IntPtr, (DisposeSentinel, Allocator)>();

        [BurstDiscard]
        public static void CreateSafety(UnsafeReference ptr, Allocator allocator, out AtomicSafetyHandle handle)
        {
            DisposeSentinel.Create(out handle, out var sentinel, 1, allocator);

            IntPtr p = ptr.IntPtr;
            m_Safety.Add(p, (sentinel, allocator));
        }
        [BurstDiscard]
        public static void RemoveSafety(UnsafeReference ptr, ref AtomicSafetyHandle handle)
        {
            IntPtr p = ptr.IntPtr;
            var sentinel = m_Safety[p];

            DisposeSentinel.Dispose(ref handle, ref sentinel.Item1);
            m_Safety.Remove(p);
        }
        [BurstDiscard]
        public static void DisposeAllSafeties()
        {
            foreach (var item in m_Safety)
            {
                UnsafeUtility.Free(item.Key.ToPointer(), item.Value.Item2);
            }
            m_Safety.Clear();
        }
#endif

        #endregion

        private static readonly Dictionary<Type, ExportFieldInfo> s_ParsedExportFields = new Dictionary<Type, ExportFieldInfo>();
        private sealed class ExportFieldInfo
        {
            public FieldInfo[] fieldInfos;
            public int size;
            public int alignment;
        }
        private struct ExportDataComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                if (x == y) return 0;
                else if (x < y) return -1;
                else return 1;
            }
        }
        private static ExportFieldInfo GetExportFieldInfo(Type t)
        {
            if (!s_ParsedExportFields.TryGetValue(t, out var fields))
            {
                var temp = t
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Where(t =>
                    {
                        if (t.GetCustomAttribute<ExportDataAttribute>() == null) return false;
                        else if (!t.IsPublic)
                        {
                            return t.GetCustomAttribute<SerializeField>() != null;
                        }
                        return t.GetCustomAttribute<NonSerializedAttribute>() == null;
                    })
                    .OrderBy(t => TypeHelper.SizeOf(t.FieldType), new ExportDataComparer())
                    .ToArray();
                int size = 0, alignment = 0;
                foreach (var item in temp)
                {
                    int itemSize = TypeHelper.SizeOf(item.FieldType);
                    size += itemSize;

                    alignment = Mathf.Max(alignment, itemSize);
                    alignment = alignment > 4 ? 4 : alignment;
                }
                fields = new ExportFieldInfo
                {
                    fieldInfos = temp,
                    size = size,
                    alignment = alignment
                };

                s_ParsedExportFields.Add(t, fields);
            }
            return fields;
        }
        public static unsafe UnsafeExportedData ExportData<T>(in T t, Allocator allocator)
            where T : unmanaged
        {
            ExportFieldInfo fields = GetExportFieldInfo(TypeHelper.TypeOf<T>.Type);

            UnsafeAllocator alloc = new UnsafeAllocator(fields.size, fields.alignment, allocator);
            UnsafeReference p = alloc.Ptr;

            foreach (FieldInfo field in fields.fieldInfos)
            {
                object obj = field.GetValue(t);
                byte[] bytes = obj.ObjectToByteArray();

                fixed (byte* ptr = bytes)
                {
                    UnsafeUtility.MemCpy(p, ptr, bytes.Length);
                    p += bytes.Length;
                }
            }

            return new UnsafeExportedData(TypeHelper.TypeOf<T>.Type.ToTypeInfo(), alloc);
        }
        public static unsafe T ReadData<T>(this in UnsafeExportedData t, int index)
            where T : unmanaged
        {
            ExportFieldInfo fields = GetExportFieldInfo(t.Type);

            UnsafeAllocator alloc = t.Allocator;

            //FieldInfo field = fields.fieldInfos[index];
            UnsafeReference p = alloc.Ptr;
            for (int i = 0; i < index; i++)
            {
                p += TypeHelper.SizeOf(fields.fieldInfos[i].FieldType);
            }

            T data = Marshal.PtrToStructure<T>(p);
            return data;
        }
    }
    [BurstCompatible]
    public struct UnsafeExportedData : IValidation, IDisposable, INativeDisposable
    {
        public readonly struct Identifier : IEmpty, IEquatable<Identifier>
        {
            private readonly Hash m_Hash;
            internal Identifier(Hash hash)
            {
                m_Hash = hash;
            }

            public bool Equals(Identifier other) => m_Hash.Equals(other.m_Hash);
            public bool IsEmpty() => m_Hash.IsEmpty();

            public static implicit operator Hash(Identifier t) => t.m_Hash;
        }

        private readonly Identifier m_ID;
        private readonly TypeInfo m_Type;
        private UnsafeAllocator m_Allocator;

        public Identifier ID => m_ID;
        public TypeInfo Type => m_Type;
        public UnsafeAllocator Allocator => m_Allocator;

        internal UnsafeExportedData(TypeInfo type, UnsafeAllocator allocator)
        {
            m_ID = new Identifier(Hash.NewHash());
            m_Type = type;
            m_Allocator = allocator;
        }

        public bool IsValid() => m_Allocator.IsCreated;
        public void Dispose()
        {
            m_Allocator.Dispose();
        }
        public JobHandle Dispose(JobHandle inputDeps)
        {
            inputDeps = m_Allocator.Dispose(inputDeps);

            return inputDeps;
        }
    }

    /// <summary>
    /// <see cref="UnsafeBufferUtility.ExportData{T}(in T, Allocator)"/> 를 통해 추출 될 수 있는 맴버를 지정합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ExportDataAttribute : Attribute
    {
    }
}
