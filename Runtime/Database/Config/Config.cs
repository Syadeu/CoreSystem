using System.Collections.Generic;
using System.IO;

namespace Syadeu.Database
{
    public sealed class Config
    {
        internal readonly ConfigLocation m_Location;
        internal readonly Converters.INIFile m_INI;

        internal readonly string m_Path;

        public string Name => Path.GetFileNameWithoutExtension(m_Path);
        public ConfigLocation Location => m_Location;

        internal Config(ConfigLocation location, string path)
        {
            m_Location = location;
            m_Path = path;
            if (!File.Exists(path))
            {
                m_INI = new Converters.INIFile(new List<ValuePair>(), new List<Converters.INIHeader>());
            }
            else
            {
                m_INI = Converters.INIInterface.Read(path);
            }
        }

        public object GetValue(string name) => m_INI.GetValue(name)?.GetValue();
        public void SetValue(string name, object value) => m_INI.SetValue(name, value);

        public object GetHeaderValue(string header, string valueName) => m_INI.GetHeader(header)?.GetValue(valueName)?.GetValue();
        public void SetHeaderValue(string header, string valueName, object value) => m_INI.GetOrCreateHeader(header).SetValue(valueName, value);

        public void Save()
        {
            Converters.INIInterface.Write(m_Path, m_INI);
        }
    }
}
