using System.Collections.Generic;

namespace Syadeu.Database
{
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
