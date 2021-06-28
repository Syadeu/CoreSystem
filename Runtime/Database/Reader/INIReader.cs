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

    [StaticManagerIntializeOnLoad]
    public sealed class ConfigLoader : StaticDataManager<ConfigLoader>
    {
        private static string m_GlobalConfigPath = Path.Combine(CoreSystemFolder.RootPath, "config.ini");
        private static string m_SubConfigPath = Path.Combine(CoreSystemFolder.RootPath, "Configs");

        private Config m_Global;
        private Config[] m_Locals;

        public static Config Global => Instance.m_Global;

        public override void OnInitialize()
        {
            m_Global = new Config(ConfigLocation.Global, m_GlobalConfigPath);
            string[] subConfigsPath = Directory.GetFiles(m_SubConfigPath);
            m_Locals = new Config[subConfigsPath.Length];
            for (int i = 0; i < m_Locals.Length; i++)
            {
                m_Locals[i] = new Config(ConfigLocation.Sub, subConfigsPath[i]);
            }
        }

        public Config 
    }
    public sealed class Config
    {
        private readonly ConfigLocation m_Location;
        private readonly INIFile m_INI;

        private readonly string m_Name;
        private readonly string m_Path;

        public ConfigLocation Location => m_Location;

        internal Config(ConfigLocation location, string path)
        {
            m_Location = location;
            m_Name = Path.GetFileNameWithoutExtension(path);
            m_Path = path;
            if (!File.Exists(path))
            {
                m_INI = new INIFile(null, null);
            }
            else
            {
                m_INI = INIReader.Read(path);
            }
        }

        public void Save()
        {
            INIReader.Write(m_Path, m_INI);
        }
    }

    public sealed class INIFile
    {
        public static INIFile Empty => new INIFile();

        internal ValuePair[] m_Values;
        internal INIHeader[] m_Headers;

        private INIFile() { }
        internal INIFile(ValuePair[] values, INIHeader[] headers)
        {
            m_Values = values;
            m_Headers = headers;
        }

        public INIFile NewValues(params ValuePair[] values)
        {
            m_Values = values;
            return this;
        }
        public INIFile NewHeaders(params INIHeader[] headers)
        {
            m_Headers = headers;
            return this;
        }
    }
    public sealed class INIHeader
    {
        public string m_Name;
        public ValuePair[] m_Values;

        internal INIHeader(string name) => m_Name = name;
    }
}
