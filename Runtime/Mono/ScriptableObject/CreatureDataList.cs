using Newtonsoft.Json;
using Syadeu.Database.CreatureData;
using Syadeu.Internal;
using System;
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
        public List<CreatureAttribute> m_Attributes;

        public override void OnInitialize()
        {
            LoadData();
        }

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

            string[] attPaths = Directory.GetFiles(CoreSystemFolder.CreatureAttributePath, jsonPostfix, SearchOption.AllDirectories);
            m_Attributes = new List<CreatureAttribute>();
            Type[] attTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<CreatureAttribute>.Type.IsAssignableFrom(other));
            for (int i = 0; i < attPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(attPaths[i]));
                Type t = attTypes.FindFor((other) => other.Name.Equals(lastFold));

                var temp = (CreatureAttribute)JsonConvert.DeserializeObject(File.ReadAllText(attPaths[i]), t);
                m_Attributes.Add(temp);
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

            if (m_Attributes != null)
            {
                string[] atts = Directory.GetFiles(CoreSystemFolder.CreatureAttributePath, jsonPostfix, SearchOption.AllDirectories);
                for (int i = 0; i < atts.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(atts[i]);
                    if (m_Attributes.Where((other) => other.Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(atts[i]);
                    }
                }

                for (int i = 0; i < m_Attributes.Count; i++)
                {
                    string attPath = Path.Combine(CoreSystemFolder.CreatureAttributePath, m_Attributes[i].GetType().Name);
                    if (!Directory.Exists(attPath)) Directory.CreateDirectory(attPath);

                    if (m_Attributes[i].Hash.Equals(Hash.Empty)) m_Attributes[i].Hash = Hash.NewHash();

                    File.WriteAllText($"{attPath}/{m_Attributes[i].Name}{json}",
                        JsonConvert.SerializeObject(m_Attributes[i], Formatting.Indented));
                }
            }
        }

        public Creature GetEntity(Hash hash) => m_Entites.FindFor((other) => other.m_Hash.Equals(hash));
        public CreatureAttribute GetAttribute(Hash hash) => m_Attributes.FindFor((other) => other.Hash.Equals(hash));
    }
}
