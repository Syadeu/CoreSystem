namespace Syadeu.Collections
{
    public static class FNV1a32
    {
        private const uint kPrime32 = 16777619;
        private const uint kOffsetBasis32 = 2166136261U;

        /// <summary>
        /// FNV1a 32-bit
        /// </summary>
        public static uint Calculate(string str)
        {
            if (str == null)
            {
                return kOffsetBasis32;
            }

            uint hashValue = kOffsetBasis32;

            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime32;
                hashValue ^= (uint)str[i];
            }

            return hashValue;
        }

        /// <summary>
        /// FNV1a 32-bit
        /// </summary>
        public static uint Calculate(byte[] data)
        {
            if (data == null)
            {
                return kOffsetBasis32;
            }

            uint hashValue = kOffsetBasis32;

            int length = data.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime32;
                hashValue ^= (uint)data[i];
            }

            return hashValue;
        }

        /// <summary>
        /// 32 bit FNV hashing algorithm is used by Wwise for mapping strings to wwise IDs.
        /// Adapted from AkFNVHash.h provided as part of the Wwise installation.
        /// </summary>
        public static uint CalculateLower(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            uint hashValue = kOffsetBasis32;

            int length = str.Length;
            for (int i = 0; i < length; i++)
            {
                hashValue *= kPrime32;

                // peform tolower as part of the hash to prevent garbage.
                char sval = str[i];
                if ((sval >= 'A') && (sval <= 'Z'))
                {
                    hashValue ^= (uint)sval + ('a' - 'A');
                }
                else
                {
                    hashValue ^= (uint)sval;
                }
            }

            return hashValue;
        }
    }
}
