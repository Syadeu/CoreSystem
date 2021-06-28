using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Graphs;

namespace Syadeu.Database
{
    public static class INIReader
    {
        private const string c_HeaderStart = "[";
        private const string c_HeaderEnd = "]";
        private const string c_Comment = "-";
        private static char[] c_ValueSeperator = new char[] { '=' };

        public static INIFile Read(string path)
        {
            INIFile ini;
            using (var rdr = File.OpenText(path))
            {
                ini = Read(rdr);
            }
            return ini;
        }
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
                ValuePair value;
                if (vs.Length == 2)
                {
                    object temp;
                    if (int.TryParse(vs[1], out int intVal)) temp = intVal;
                    else if (float.TryParse(vs[1], out float floatVal)) temp = floatVal;
                    else if (bool.TryParse(vs[1], out bool boolVal)) temp = boolVal;
                    else temp = vs[1];

                    $"new value: {vs[0]}: {temp}".ToLog();
                    value = ValuePair.New(vs[0], temp);
                }
                else
                {
                    $"null value: {vs[0]}".ToLog();
                    value = new ValueNull(vs[0]);
                }

                if (currentHeader != null)
                {
                    headerValues.Add(value);
                }
                else
                {
                    values.Add(value);
                }
            }

            void EndHeader()
            {
                if (currentHeader != null)
                {
                    currentHeader.m_Values = headerValues;
                    headers.Add(currentHeader);
                    currentHeader = null;
                    headerValues = null;
                }
            }

            return new INIFile(values, headers);
        }
        public static void Write(string path, INIFile ini)
        {
            using (System.IO.Stream stream = File.OpenWrite(path))
            {
                using (var writer = new System.IO.StreamWriter(stream))
                {
                    Write(writer, ini);
                }
            }
        }
        public static void Write(TextWriter wr, INIFile ini)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ini.m_Values.Count; i++)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(ToValuePairLine(ini.m_Values[i]));
            }

            for (int i = 0; i < ini.m_Headers.Count; i++)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(string.Concat(c_HeaderStart, ini.m_Headers[i].m_Name, c_HeaderEnd));

                for (int j = 0; j < ini.m_Headers[i].m_Values.Count; j++)
                {
                    sb.AppendLine(ToValuePairLine(ini.m_Headers[i].m_Values[j]));
                }
            }

            wr.Write(sb.ToString());

            static string ToValuePairLine(ValuePair value)
            {
                if (value is ValueNull)
                {
                    return string.Concat(c_Comment, value.Name);
                }
                return string.Concat(value.Name, c_ValueSeperator[0], value.GetValue().ToString());
            }
        }
    }
}
