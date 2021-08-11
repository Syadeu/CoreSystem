using Syadeu.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Syadeu.Database
{
    //[Obsolete]
    //public sealed class Config
    //{
    //    internal readonly ConfigLocation m_Location;
    //    internal readonly Converters.INIFile m_INI;

    //    internal readonly string m_Path;

    //    public string Name => Path.GetFileNameWithoutExtension(m_Path);
    //    public ConfigLocation Location => m_Location;

    //    internal Config(ConfigLocation location, string path)
    //    {
    //        m_Location = location;
    //        m_Path = path;
    //        if (!File.Exists(path))
    //        {
    //            m_INI = new Converters.INIFile(new List<ValuePair>(), new List<Converters.INIHeader>());
    //        }
    //        else
    //        {
    //            m_INI = Converters.INIInterface.Read(path);
    //        }
    //    }

    //    public object GetValue(string name) => m_INI.GetValue(name)?.GetValue();
    //    public void SetValue(string name, object value) => m_INI.SetValue(name, value);

    //    public object GetHeaderValue(string header, string valueName) => m_INI.GetHeader(header)?.GetValue(valueName)?.GetValue();
    //    public void SetHeaderValue(string header, string valueName, object value) => m_INI.GetOrCreateHeader(header).SetValue(valueName, value);

    //    public void Save()
    //    {
    //        Converters.INIInterface.Write(m_Path, m_INI);
    //    }
    //}

    public sealed class Config
    {
        private const string c_HeaderStart = "[";
        private const string c_HeaderEnd = "]";
        private const string c_Comment = ";";
        private static readonly char[] s_ValueSeperator = new char[] { '=' };

        private const string c_KeyValue = "{0} = {1}";

        public enum ValueType
        {
            Header,

            String,
            Integer,
            Single,
            Boolen
        }
        public abstract class ConfigValueBase
        {
            public abstract string Name { get; set; }
            public abstract ValueType Type { get; }

            public abstract object GetValue();
            public abstract void SetValue(object value);
        }
        public sealed class StringValue : ConfigValueBase
        {
            public override string Name { get; set; }
            public override ValueType Type => ValueType.String;

            public string Value { get; set; }

            public override object GetValue() => Value;
            public override void SetValue(object value) => Value = value.ToString();
            public override string ToString() => string.Format(c_KeyValue, Name, Value);
        }
        public sealed class IntValue : ConfigValueBase
        {
            public override string Name { get; set; }
            public override ValueType Type => ValueType.Integer;

            public int Value { get; set; }

            public override object GetValue() => Value;
            public override void SetValue(object value) => Value = int.Parse(value.ToString());
            public override string ToString() => string.Format(c_KeyValue, Name, Value);
        }
        public sealed class SingleValue : ConfigValueBase
        {
            public override string Name { get; set; }
            public override ValueType Type => ValueType.Single;

            public float Value { get; set; }

            public override object GetValue() => Value;
            public override void SetValue(object value) => Value = float.Parse(value.ToString());
            public override string ToString() => string.Format(c_KeyValue, Name, Value);
        }
        public sealed class BooleanValue : ConfigValueBase
        {
            public override string Name { get; set; }
            public override ValueType Type => ValueType.Boolen;

            public bool Value { get; set; }

            public override object GetValue() => Value;
            public override void SetValue(object value) => Value = bool.Parse(value.ToString());
            public override string ToString() => string.Format(c_KeyValue, Name, Value);
        }
        public sealed class ConfigHeader : ConfigValueBase
        {
            public override string Name { get; set; }
            public override ValueType Type => ValueType.Header;

            public Dictionary<string, ConfigValueBase> Values { get; set; }

            public ConfigValueBase GetOrCreateValue(Type t, string name)
            {
                if (!Values.TryGetValue(name, out ConfigValueBase value))
                {
                    if (t.Equals(TypeHelper.TypeOf<int>.Type)) value = new IntValue() { Name = name };
                    else if (t.Equals(TypeHelper.TypeOf<float>.Type)) value = new SingleValue() { Name = name };
                    else if (t.Equals(TypeHelper.TypeOf<double>.Type)) value = new SingleValue() { Name = name };
                    else if (t.Equals(TypeHelper.TypeOf<bool>.Type)) value = new BooleanValue() { Name = name };
                    else value = new StringValue() { Name = name };

                    Values.Add(name, value);
                }

                return value;
            }

            public override object GetValue() => throw new NotImplementedException(nameof(ConfigHeader.GetValue));
            public override void SetValue(object value) => throw new NotImplementedException(nameof(ConfigHeader.SetValue));
            public override string ToString()
            {
                const string c_Header = "[{0}]";

                string output = string.Format(c_Header, Name);
                foreach (ConfigValueBase item in Values.Values)
                {
                    output += "\n" + item.ToString();
                }
                return output;
            }
        }

        private Dictionary<string, ConfigValueBase> Values;

        public string Name { get; }
        public int Count => Values.Count;

        public Config(string name, TextReader rdr)
        {
            Name = name;
            Values = new Dictionary<string, ConfigValueBase>();

            string line = string.Empty;
            while (rdr.Peek() > 0)
            {
                line = rdr.ReadLine();
                if (string.IsNullOrEmpty(line) ||
                    line.StartsWith(c_Comment)) continue;

                if (line.StartsWith(c_HeaderStart))
                {
                    string headerTxt = line.Substring(1, line.Length - 2).Trim();
                    ConfigHeader header = MakeHeader(ref line, headerTxt, rdr);

                    Values.Add(headerTxt, header);
                    if (rdr.Peek() < 0) break;
                }

                string[] split = line.Split(s_ValueSeperator);
                if (split.Length != 2)
                {
                    $"invalid config value pair : {line}".ToLog();
                    continue;
                }
                Values.Add(split[0].Trim(), ToValue(split[0].Trim(), split[1].Trim()));


                var temp = Values[split[0].Trim()];
                $"added {temp.Name}({temp.Type}) : {temp.GetValue()}".ToLog();
            }
        }
        public Config(string name)
        {
            Name = name;
            Values = new Dictionary<string, ConfigValueBase>();
        }
        private ConfigHeader MakeHeader(ref string line, string headerTxt, TextReader rdr)
        {
            ConfigHeader header = new ConfigHeader()
            {
                Name = headerTxt,
                Values = new Dictionary<string, ConfigValueBase>()
            };

            while (rdr.Peek() > 0)
            {
                line = rdr.ReadLine();
                if (line.StartsWith(c_Comment))
                {
                    continue;
                }
                else if (string.IsNullOrEmpty(line) ||
                        line.StartsWith(c_HeaderStart)) break;

                string[] split = line.Split(s_ValueSeperator);
                if (split.Length != 2)
                {
                    $"invalid config value pair : {line}".ToLog();
                    continue;
                }

                header.Values.Add(split[0].Trim(), ToValue(split[0].Trim(), split[1].Trim()));

                var temp = header.Values[split[0].Trim()];
                $"added {temp.Name}({temp.Type}) : {temp.GetValue()}".ToLog();
            }

            return header;
        }
        private ConfigValueBase ToValue(string name, string value)
        {
            ConfigValueBase temp;

            if (int.TryParse(value, out int resultInt))
            {
                temp = new IntValue()
                {
                    Name = name,
                    Value = resultInt
                };
            }
            else if (float.TryParse(value, out float resultSingle))
            {
                temp = new SingleValue()
                {
                    Name = name,
                    Value = resultSingle
                };
            }
            else if (double.TryParse(value, out double resultDouble))
            {
                temp = new SingleValue()
                {
                    Name = name,
                    Value = (float)resultDouble
                };
            }
            else if (bool.TryParse(value, out bool resultBoolen))
            {
                temp = new BooleanValue()
                {
                    Name = name,
                    Value = resultBoolen
                };
            }
            else
            {
                temp = new StringValue()
                {
                    Name = name,
                    Value = value
                };
            }

            return temp;
        }

        public ConfigValueBase GetOrCreateValue(Type t, string name)
        {
            if (!Values.TryGetValue(name, out ConfigValueBase value))
            {
                if (t.Equals(TypeHelper.TypeOf<int>.Type)) value = new IntValue() { Name = name };
                else if (t.Equals(TypeHelper.TypeOf<float>.Type)) value = new SingleValue() { Name = name };
                else if (t.Equals(TypeHelper.TypeOf<double>.Type)) value = new SingleValue() { Name = name };
                else if (t.Equals(TypeHelper.TypeOf<bool>.Type)) value = new BooleanValue() { Name = name };
                else value = new StringValue() { Name = name };

                Values.Add(name, value);
            }

            return value;
        }
        public ConfigHeader GetOrCreateHeader(string header)
        {
            if (!Values.TryGetValue(header, out ConfigValueBase value))
            {
                value = new ConfigHeader() { Name = header, Values = new Dictionary<string, ConfigValueBase>() };
                Values.Add(header, value);
            }

            return (ConfigHeader)value;
        }

        public override string ToString()
        {
            string output = string.Empty;
            foreach (ConfigValueBase item in Values.Values)
            {
                if (!string.IsNullOrEmpty(output)) output += "\n";
                output += item.ToString();
            }
            return output;
        }
    }
}
