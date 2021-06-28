using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                    return string.Concat(c_Comment, value.m_Name);
                }
                return string.Concat(value.m_Name, c_ValueSeperator[0], value.GetValue().ToString());
            }
        }
    }

    [StaticManagerIntializeOnLoad]
    public sealed class ConfigLoader : StaticDataManager<ConfigLoader>
    {
        private static string m_GlobalConfigPath = Path.Combine(CoreSystemFolder.CoreSystemDataPath, "config.ini");
        private static string m_SubConfigPath = Path.Combine(CoreSystemFolder.CoreSystemDataPath, "Configs");

        private Config m_Global;
        private Dictionary<string, Config> m_Locals;

        public static Config Global => Instance.m_Global;

        public override void OnInitialize()
        {
            if (!Directory.Exists(m_SubConfigPath)) Directory.CreateDirectory(m_SubConfigPath);

            m_Global = new Config(ConfigLocation.Global, m_GlobalConfigPath);
            string[] subConfigsPath = Directory.GetFiles(m_SubConfigPath);
            m_Locals = new Dictionary<string, Config>();
            for (int i = 0; i < subConfigsPath.Length; i++)
            {
                Config config = new Config(ConfigLocation.Sub, subConfigsPath[i]);
                m_Locals.Add(config.Name, config);
            }
        }

        public static void LoadConfig(object obj)
        {
            System.Type t = obj.GetType();
            var configAtt = t.GetCustomAttribute<RequireGlobalConfigAttribute>();
            if (configAtt == null) return;

            Config config;
            if (configAtt.m_Location == ConfigLocation.Global) config = Global;
            else
            {
                if (!Instance.m_Locals.TryGetValue(configAtt.m_Name, out config))
                {
                    config = new Config(ConfigLocation.Sub, 
                        Path.Combine(m_SubConfigPath, configAtt.m_Name + ".ini"));
                    Instance.m_Locals.Add(config.Name, config);
                }
            }

            FieldInfo[] fields = t
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where((other) => other.GetCustomAttribute<ConfigValueAttribute>() != null)
                .ToArray();

            for (int i = 0; i < fields.Length; i++)
            {
                var att = fields[i].GetCustomAttribute<ConfigValueAttribute>();
                object value;
                if (string.IsNullOrEmpty(att.Header))
                {
                    value = config.INIFile
                        .GetOrCreateValue(fields[i].FieldType, fields[i].Name)
                        .GetValue();
                }
                else
                {
                    value = config.INIFile.GetOrCreateHeader(att.Header)
                        .GetOrCreateValue(fields[i].FieldType, fields[i].Name)
                        .GetValue();
                }
                $"{fields[i].Name}: {value}".ToLog();
                fields[i].SetValue(obj, value);
            }

            config.Save();
        }
    }
    public sealed class Config
    {
        internal readonly ConfigLocation m_Location;
        private readonly INIFile m_INI;

        internal readonly string m_Path;

        public string Name => Path.GetFileNameWithoutExtension(m_Path);
        public ConfigLocation Location => m_Location;
        public INIFile INIFile => m_INI;

        internal Config(ConfigLocation location, string path)
        {
            m_Location = location;
            m_Path = path;
            if (!File.Exists(path))
            {
                m_INI = new INIFile(new List<ValuePair>(), new List<INIHeader>());
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

        internal List<ValuePair> m_Values;
        internal List<INIHeader> m_Headers;

        private INIFile() { }
        internal INIFile(List<ValuePair> values, List<INIHeader> headers)
        {
            m_Values = values;
            m_Headers = headers;
        }

        public ValuePair GetValue(string name)
        {
            for (int i = 0; i < m_Values.Count; i++)
            {
                if (m_Values[i].m_Name.Equals(name)) return m_Values[i];
            }
            return null;
        }
        public INIHeader GetHeader(string name)
        {
            for (int i = 0; i < m_Headers.Count; i++)
            {
                if (m_Headers[i].m_Name.Equals(name)) return m_Headers[i];
            }
            return null;
        }

        public ValuePair GetOrCreateValue<T>(string name) where T : System.IConvertible
            => GetOrCreateValue(typeof(T), name);
        public ValuePair GetOrCreateValue(System.Type type, string name)
        {
            ValuePair value = GetValue(name);
            if (value != null) return value;

            value = ValuePair.New(name, System.Activator.CreateInstance(type));
            m_Values.Add(value);
            return value;
        }
        public void SetValue(string name, object value)
        {
            ValuePair temp = ValuePair.New(name, value);
            for (int i = 0; i < m_Values.Count; i++)
            {
                if (m_Values[i].m_Name.Equals(name))
                {
                    m_Values[i] = temp;
                    return;
                }
            }

            m_Values.Add(temp);
        }
        public INIHeader GetOrCreateHeader(string name)
        {
            INIHeader header = GetHeader(name);
            if (header != null) return header;

            header = new INIHeader(name);
            m_Headers.Add(header);
            return header;
        }
    }
    public sealed class INIHeader
    {
        public string m_Name;
        public List<ValuePair> m_Values;

        internal INIHeader(string name)
        {
            m_Name = name;
            m_Values = new List<ValuePair>();
        }

        public ValuePair GetValue(string name)
        {
            for (int i = 0; i < m_Values.Count; i++)
            {
                if (m_Values[i].m_Name.Equals(name)) return m_Values[i];
            }
            return null;
        }
        public ValuePair GetOrCreateValue<T>(string name) where T : System.IConvertible
            => GetOrCreateValue(typeof(T), name);
        public ValuePair GetOrCreateValue(System.Type type, string name)
        {
            ValuePair value = GetValue(name);
            if (value != null) return value;

            value = ValuePair.New(name, System.Activator.CreateInstance(type));
            m_Values.Add(value);
            return value;
        }
        public void SetValue(string name, object value)
        {
            ValuePair temp = ValuePair.New(name, value);
            for (int i = 0; i < m_Values.Count; i++)
            {
                if (m_Values[i].m_Name.Equals(name))
                {
                    m_Values[i] = temp;
                    return;
                }
            }

            m_Values.Add(temp);
        }
    }
}
