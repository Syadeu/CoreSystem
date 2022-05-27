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
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections.IO
{
    public static class NativeReaderExtensionMethods
    {
        //[BurstDiscard]
        //public static string ToString(this INativeReader rdr, Encoding encoding)
        //{
        //    unsafe
        //    {
        //        return encoding.GetString(rdr.Data, (int)rdr.Size);
        //    }
        //}
        //public static NativeArray<byte> ToByte(this INativeReader rdr, Allocator allocator)
        //{
        //    unsafe
        //    {
        //        return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(rdr.Data, (int)rdr.Size, allocator);
        //    }
        //}
//        public static void ToByteWithoutAllocation(this INativeReader rdr, NativeArray<byte> bytes)
//        {
//#if DEBUG_MODE
//            if (bytes.Length < rdr.Size)
//            {
//                throw new Exception("out of range");
//            }
//#endif
//            unsafe
//            {
//                void* p = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(bytes);
//                UnsafeUtility.MemCpy(p, rdr.Data, rdr.Size);
//            }
//        }

        //public T Read<T>()
        //{
        //    Encoding.UTF8

        //    MemoryStream memStream = new MemoryStream();
        //    BinaryFormatter binForm = new BinaryFormatter();
        //    memStream.Write(arrBytes, 0, arrBytes.Length);
        //    memStream.Seek(0, SeekOrigin.Begin);
        //    Object obj = (Object)binForm.Deserialize(memStream);
        //}
    }
}
