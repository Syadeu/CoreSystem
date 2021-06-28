using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Syadeu.Database
{
    public static class INIReader
    {
        private const string c_HeaderStart = "[";
        private const string c_HeaderEnd = "]";
        private const string c_Comment = "-";
        private static char[] c_ValueSeperator = new char[] { '=' };

        public static INIFile Read(TextReader rdr)
        {
            List<INIHeader> headers = new List<INIHeader>();
            List<ValuePair> values = new List<ValuePair>();
            
            INIHeader currentHeader = null;
            List<ValuePair> headerValues = null;

            while (true)
            {
                string line = rdr.ReadLine();
                if (line == null) break;

                line = line.Trim();
                if (line.StartsWith(c_Comment)) continue;
                if (string.IsNullOrEmpty(line))
                {
                    EndHeader();
                    continue;
                }

                if (line.StartsWith(c_HeaderStart))
                {
                    EndHeader();
                    string headerName = line.Substring(1, line.Length - 2).Trim();
                    currentHeader = new INIHeader(headerName);
                    headerValues = new List<ValuePair>();

                    continue;
                }

                string[] vs = line.Split(c_ValueSeperator, 2);
                if (currentHeader != null)
                {
                    headerValues.Add(vs.Length == 2 ? ValuePair.New(vs[0], vs[1]) : new ValueNull(vs[0]));
                }
                else
                {
                    values.Add(vs.Length == 2 ? ValuePair.New(vs[0], vs[1]) : new ValueNull(vs[0]));
                }
            }

            void EndHeader()
            {
                if (currentHeader != null)
                {
                    currentHeader.m_Values = headerValues.ToArray();
                    headers.Add(currentHeader);
                    currentHeader = null;
                    headerValues = null;
                }
            }

            return new INIFile(values.ToArray(), headers.ToArray());
        }
        public static void Write(TextWriter wr, INIFile ini)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ini.m_Values.Length; i++)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(ToValuePairLine(ini.m_Values[i]));
            }

            for (int i = 0; i < ini.m_Headers.Length; i++)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(string.Concat(c_HeaderStart, ini.m_Headers[i].m_Name, c_HeaderEnd));

                for (int j = 0; j < ini.m_Headers[i].m_Values.Length; j++)
                {
                    sb.AppendLine(ToValuePairLine(ini.m_Headers[i].m_Values[j]));
                }
            }

            wr.Write(sb.ToString());

            static string ToValuePairLine(ValuePair value)
            {
                if (value is ValueNull)
                {
                    return string.Concat(c_Comment, value.m_Name);
                }
                return string.Concat(value.m_Name, c_ValueSeperator[0], value.GetValue().ToString());
            }
        }
    }

    public sealed class INIFile
    {
        public static INIFile Empty = new INIFile();

        public ValuePair[] m_Values;
        public INIHeader[] m_Headers;

        private INIFile() { }
        internal INIFile(ValuePair[] values, INIHeader[] headers)
        {
            m_Values = values;
            m_Headers = headers;
        }


    }
    public sealed class INIHeader
    {
        public string m_Name;
        public ValuePair[] m_Values;

        internal INIHeader(string name) => m_Name = name;
    }

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
