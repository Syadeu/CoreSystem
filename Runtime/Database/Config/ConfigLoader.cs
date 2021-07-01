using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Syadeu.Database
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
                    value = config.m_INI
                        .GetOrCreateValue(fields[i].FieldType, fields[i].Name)
                        .GetValue();
                }
                else
                {
                    value = config.m_INI.GetOrCreateHeader(att.Header)
                        .GetOrCreateValue(fields[i].FieldType, fields[i].Name)
                        .GetValue();
                }
                $"{fields[i].Name}: {value}".ToLog();
                fields[i].SetValue(obj, value);
            }

            config.Save();
        }
    }
}
