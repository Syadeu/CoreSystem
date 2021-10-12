namespace Syadeu.Collections
{
    public static class FNV1a64
    {
        private const ulong kPrime64 = 1099511628211LU;
        private const ulong kOffsetBasis64 = 14695981039346656037LU;

        /// <summary>
        /// FNV1a 64-bit
        /// </summary>
        public static ulong Calculate(string str)
        {
            if (str == null)
            {
                return kOffsetBasis64;
            }

            ulong hashValue = kOffsetBasis64;

            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime64;
                hashValue ^= (ulong)str[i];
            }

            return hashValue;
        }

        /// <summary>
        /// FNV1a 64-bit
        /// </summary>
        public static ulong Calculate(byte[] data)
        {
            if (data == null)
            {
                return kOffsetBasis64;
            }

            ulong hashValue = kOffsetBasis64;

            int length = data.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime64;
                hashValue ^= (ulong)data[i];
            }

            return hashValue;
        }
    }
}
