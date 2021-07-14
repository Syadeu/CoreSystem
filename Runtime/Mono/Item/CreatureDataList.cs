using Newtonsoft.Json;
using Syadeu.Database.CreatureData;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu/Creature")]
    public sealed class CreatureDataList : StaticSettingEntity<CreatureDataList>
    {
        const string jsonPostfix = "*.json";
        const string json = ".json";

        public List<Creature> m_Entites;
        public List<ICreatureAttribute> m_Attributes;

        public void LoadData()
        {
            if (!Directory.Exists(CoreSystemFolder.CreaturePath)) Directory.CreateDirectory(CoreSystemFolder.CreaturePath);
            if (!Directory.Exists(CoreSystemFolder.CreatureAttributePath)) Directory.CreateDirectory(CoreSystemFolder.CreatureAttributePath);

            string[] entityPaths = Directory.GetFiles(CoreSystemFolder.CreaturePath, jsonPostfix, SearchOption.TopDirectoryOnly);
            m_Entites = new List<Creature>();
            for (int i = 0; i < entityPaths.Length; i++)
            {
                m_Entites.Add(JsonConvert.DeserializeObject<Creature>(File.ReadAllText(entityPaths[i])));
            }
        }
        public void SaveData()
        {
            if (!Directory.Exists(CoreSystemFolder.CreaturePath)) Directory.CreateDirectory(CoreSystemFolder.CreaturePath);
            if (!Directory.Exists(CoreSystemFolder.CreatureAttributePath)) Directory.CreateDirectory(CoreSystemFolder.CreatureAttributePath);

            if (m_Entites != null)
            {
                string[] entityPaths = Directory.GetFiles(CoreSystemFolder.CreaturePath, jsonPostfix, SearchOption.TopDirectoryOnly);
                for (int i = 0; i < entityPaths.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(entityPaths[i]);
                    if (m_Entites.Where((other) => other.m_Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(entityPaths[i]);
                    }
                }
                for (int i = 0; i < m_Entites.Count; i++)
                {
                    if (m_Entites[i].m_Hash.Equals(Hash.Empty)) m_Entites[i].m_Hash = Hash.NewHash();

                    File.WriteAllText($"{CoreSystemFolder.CreaturePath}/{m_Entites[i].m_Name}{json}",
                        JsonConvert.SerializeObject(m_Entites[i], Formatting.Indented));
                }
            }
        }

        public Creature GetEntity(Hash hash)
        {
            for (int i = 0; i < m_Entites.Count; i++)
            {
                if (m_Entites[i].m_Hash.Equals(hash)) return m_Entites[i];
            }
            return null;
        }
    }
}
