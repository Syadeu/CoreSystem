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

using Syadeu.Collections.Buffer.LowLevel;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Collections
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
    internal static class CheckSumMathematics
    {
        public static uint Calculate(byte[] data)
        {
            uint output;
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    output = GetSum(ptr, data.Length);
                }
                
                // https://pretagteam.com/question/converting-c-byte-to-bitarray
                //if (BitConverter.IsLittleEndian)
                output = RemoveHighNibble(in output);

                // Complement
                output = Complement(in output) + 1;
            }

            return output;
        }
        public static uint Calculate<T>(T data) where T : unmanaged
        {
            uint output;
            unsafe
            {
                output = GetSum((byte*)&data, UnsafeUtility.SizeOf<T>());

                // https://pretagteam.com/question/converting-c-byte-to-bitarray
                //if (BitConverter.IsLittleEndian)
                output = RemoveHighNibble(in output);

                // Complement
                output = Complement(in output) + 1;
            }

            return output;
        }
        public static uint Calculate<T>(in UnsafeReference<T> buffer, in int length)
            where T : unmanaged
        {
            uint output;
            unsafe
            {
                byte* bytes = UnsafeBufferUtility.AsBytes(ref buffer.Value, out int byteLength);

                output = GetSum(bytes, byteLength);

                // https://pretagteam.com/question/converting-c-byte-to-bitarray
                //if (BitConverter.IsLittleEndian)
                output = RemoveHighNibble(in output);

                // Complement
                output = Complement(in output) + 1;
            }

            return output;
        }

        public static uint Validate(byte[] data, in uint checkSum)
        {
            uint output;
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    output = GetSum(ptr, data.Length);
                }
                output += checkSum;

                // https://pretagteam.com/question/converting-c-byte-to-bitarray
                //if (BitConverter.IsLittleEndian)
                output = RemoveHighNibble(in output);
            }

            return output;
        }
        /// <summary>
        /// CheckSum(<paramref name="checkSum"/>)으로 해당 데이터가 올바른지 검사합니다.
        /// </summary>
        /// <remarks>
        /// <paramref name="checkSum"/> 은 <seealso cref="CheckSum"/> 을 통해 연산될 수 있습니다.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="checkSum"></param>
        /// <returns></returns>
        public static uint Validate<T>(T data, in uint checkSum) where T : unmanaged
        {
            uint output;
            unsafe
            {
                output = GetSum((byte*)&data, UnsafeUtility.SizeOf<T>());
                output += checkSum;

                // https://pretagteam.com/question/converting-c-byte-to-bitarray
                //if (BitConverter.IsLittleEndian)
                output = RemoveHighNibble(in output);
            }

            return output;
        }

        [BurstCompile]
        private static uint GetSum(in UnsafeReference<byte> bytes, int size)
        {
            uint output = 0;
            for (int i = 0; i < size; i++)
            {
                output += bytes[i];
            }
            return output;
        }
        [BurstCompile]
        private static unsafe uint RemoveHighNibble(in uint sum)
        {
            uint output = sum;
            byte* bytes = (byte*)&output;

            for (int i = 3; i >= 0; i--)
            {
                if (bytes[i] == 0x00) continue;
                else if ((bytes[i] & 0xF0) != 0)
                {
                    bytes[i] &= 0x0F;
                    break;
                }
                else if ((bytes[i] & 0x0F) != 0)
                {
                    bytes[i] = 0;
                    break;
                }
            }

            return output;
        }
        [BurstCompile]
        private static uint Complement(in uint value)
        {
            BitField32 bitField32 = new BitField32(value);

            int endIndex = 31;
            while (endIndex >= 0 && !bitField32.IsSet(endIndex))
            {
                endIndex--;
            }

            for (int i = 0; i <= endIndex; i++)
            {
                bitField32.SetBits(i, !bitField32.IsSet(i));
            }

            return bitField32.Value;
        }
    }
}
