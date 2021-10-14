using Syadeu.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Syadeu.Collections
{
    [StaticManagerIntializeOnLoad]
    [StaticManagerDescription(
        "Config Loader is handling and manganed .ini files,\n" +
        "in the outside of application.datapath folder.\n" +
        "\n" +
        "You can save the CoreSystem's manager values with just attaching\n" +
        "RequireGlobalConfig attribute.\n" +
        "\n" +
        "Main config name is config.ini in the top of application root folder,\n" +
        "and the sub configs will be inside of /CoreSystem/Configs" +
        "")]
    [UnityEngine.AddComponentMenu("")]
    public sealed class ConfigLoader : StaticDataManager<ConfigLoader>
    {
        private static string m_GlobalConfigPath = Path.Combine(CoreSystemFolder.CoreSystemDataPath, "config.ini");
        private static string m_SubConfigPath = Path.Combine(CoreSystemFolder.CoreSystemDataPath, "Configs");
        const string c_Ext = ".ini";

        private Config m_Global;
        private Dictionary<Hash, Config> m_Locals;

        public static Config Global => Instance.m_Global;

        public override void OnInitialize()
        {
            if (!Directory.Exists(m_SubConfigPath)) Directory.CreateDirectory(m_SubConfigPath);

            if (File.Exists(m_GlobalConfigPath))
            {
                using (var rdr = File.OpenText(m_GlobalConfigPath))
                {
                    m_Global = new Config(m_GlobalConfigPath, rdr);

                    CoreSystem.Logger.Log(Channel.Core, $"Config {m_Global.Name} loaded");
                }
            }
            else m_Global = new Config("config");

            string[] subConfigsPath = Directory.GetFiles(m_SubConfigPath);
            m_Locals = new Dictionary<Hash, Config>();
            for (int i = 0; i < subConfigsPath.Length; i++)
            {
                using (var rdr = File.OpenText(subConfigsPath[i]))
                {
                    Config config = new Config(Path.GetFileNameWithoutExtension(subConfigsPath[i]), rdr);
                    m_Locals.Add(Hash.NewHash(config.Name), config);

                    CoreSystem.Logger.Log(Channel.Core, $"Config {config.Name} loaded");
                }
            }
        }

        public static void Save()
        {
            if (Instance.m_Global.Count > 0)
            {
                using (var stream = File.Open(m_GlobalConfigPath, FileMode.OpenOrCreate))
                using (var wr = new StreamWriter(stream))
                {
                    wr.Write(Instance.m_Global.ToString());

                    CoreSystem.Logger.Log(Channel.Core, $"Config({Instance.m_Global.Name}) saved");
                }
            }

            foreach (var item in Instance.m_Locals.Values)
            {
                if (item.Count > 0)
                {
                    string path = Path.Combine(m_SubConfigPath, item.Name + c_Ext);

                    using (var stream = File.Open(path, FileMode.OpenOrCreate))
                    using (var wr = new StreamWriter(stream))
                    {
                        wr.Write(item.ToString());
                    }

                    CoreSystem.Logger.Log(Channel.Core, $"Config({item.Name}) saved");
                }
            }
        }
        public static T LoadConfigValue<T>(string name, string field)
            => LoadConfigValue<T>(name, string.Empty, field);
        public static T LoadConfigValue<T>(string name, string header, string field)
        {
            const string c_GlobalConfig = "config";

            name = name.Trim(); header = header.Trim(); field = field.Trim();

            Config config;
            if (name.Equals(c_GlobalConfig)) config = Global;
            else
            {
                Hash hash = Hash.NewHash(name);
                if (!Instance.m_Locals.TryGetValue(hash, out config))
                {
                    config = new Config(name);
                    Instance.m_Locals.Add(hash, config);
                }
            }

            Config.ConfigValueBase valueBase;
            if (string.IsNullOrEmpty(header))
            {
                valueBase = config
                    .GetOrCreateHeader(header)
                    .GetOrCreateValue(TypeHelper.TypeOf<T>.Type, field, null);
            }
            else valueBase = config.GetOrCreateValue(TypeHelper.TypeOf<T>.Type, field, null);

            if (!TypeHelper.TypeOf<T>.Type.Equals(valueBase.GetValue().GetType()))
            {
                CoreSystem.Logger.LogError(Channel.Data,
                    "Request config value type not match. " +
                    $"Requested {TypeHelper.TypeOf<T>.Name} but Expected {valueBase.GetValue().GetType()}");
            }
            return (T)valueBase.GetValue();
        }
        public static void LoadConfig(object obj)
        {
            System.Type t = obj.GetType();
            var configAtt = t.GetCustomAttribute<RequireGlobalConfigAttribute>();
            if (configAtt == null) return;
            CoreSystem.Logger.Log(Channel.Core, $"Config loading for {t.Name}");

            Config config;
            if (configAtt.m_Location == ConfigLocation.Global) config = Global;
            else
            {
                string name;
                if (string.IsNullOrEmpty(configAtt.m_Name)) name = t.Name;
                else name = configAtt.m_Name;

                Hash hash = Hash.NewHash(name);
                if (!Instance.m_Locals.TryGetValue(hash, out config))
                {
                    config = new Config(name);
                    Instance.m_Locals.Add(hash, config);
                }
            }

            FieldInfo[] fields = t
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where((other) => other.GetCustomAttribute<ConfigValueAttribute>() != null)
                .ToArray();

            for (int i = 0; i < fields.Length; i++)
            {
                ConfigValueAttribute att = fields[i].GetCustomAttribute<ConfigValueAttribute>();
                Config.ConfigValueBase value;
                if (string.IsNullOrEmpty(att.Header))
                {
                    value = config
                        .GetOrCreateValue(
                            fields[i].FieldType, 
                            string.IsNullOrEmpty(att.Name) ? fields[i].Name : att.Name,
                            att);
                }
                else
                {
                    value = config
                        .GetOrCreateHeader(att.Header)
                        .GetOrCreateValue(
                            fields[i].FieldType, 
                            string.IsNullOrEmpty(att.Name) ? fields[i].Name : att.Name,
                            att);
                }

                object targetValue = value.GetValue();
                if (!targetValue.GetType().Equals(fields[i].FieldType))
                {
                    CoreSystem.Logger.LogError(Channel.Core,
                        $"Config({config.Name}) has an invalid value nameof({value.Name}, {value.Type}). Expected as {fields[i].FieldType.Name} but {targetValue.GetType().Name}. Request ignored.");
                    continue;
                }
                fields[i].SetValue(obj, value.GetValue());
            }

            CoreSystem.Logger.Log(Channel.Core, $"Config loaded for {t.Name}");
        }
    }
}
