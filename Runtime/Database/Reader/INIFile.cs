using System.Collections.Generic;

namespace Syadeu.Database
{
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
                if (m_Values[i].Name.Equals(name)) return m_Values[i];
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
                if (m_Values[i].Name.Equals(name))
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
}
