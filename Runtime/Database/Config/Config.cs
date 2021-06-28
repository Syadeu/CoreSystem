using System.Collections.Generic;
using System.IO;

namespace Syadeu.Database
{
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
}
