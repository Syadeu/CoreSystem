using Newtonsoft.Json;
using Syadeu.Database.CreatureData;
using Syadeu.Database.CreatureData.Attributes;
using Syadeu.Internal;
using Syadeu.Presentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu")]
    public sealed class EntityDataList : StaticSettingEntity<EntityDataList>
    {
        const string jsonPostfix = "*.json";
        const string json = ".json";

        public List<EntityBase> m_Entites;
        public List<AttributeBase> m_Attributes;

        public override void OnInitialize()
        {
            LoadData();
        }

        public void LoadData()
        {
            if (!Directory.Exists(CoreSystemFolder.EntityPath)) Directory.CreateDirectory(CoreSystemFolder.EntityPath);
            if (!Directory.Exists(CoreSystemFolder.AttributePath)) Directory.CreateDirectory(CoreSystemFolder.AttributePath);

            string[] entityPaths = Directory.GetFiles(CoreSystemFolder.EntityPath, jsonPostfix, SearchOption.AllDirectories);
            m_Entites = new List<EntityBase>();
            Type[] entityTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<EntityBase>.Type.IsAssignableFrom(other));
            for (int i = 0; i < entityPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(entityPaths[i]));
                Type t = entityTypes.FindFor((other) => other.Name.Equals(lastFold));

                var obj = JsonConvert.DeserializeObject(File.ReadAllText(entityPaths[i]), t);
                if (!(obj is EntityBase))
                {
                    CoreSystem.Logger.LogWarning(Channel.Creature, $"Entity({t?.Name}) at {entityPaths[i]} is invalid. This entity has been ignored");
                    continue;
                }

                var temp = (EntityBase)obj;
                m_Entites.Add(temp);
            }

            string[] attPaths = Directory.GetFiles(CoreSystemFolder.AttributePath, jsonPostfix, SearchOption.AllDirectories);
            m_Attributes = new List<AttributeBase>();
            Type[] attTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(other));
            for (int i = 0; i < attPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(attPaths[i]));
                Type t = attTypes.FindFor((other) => other.Name.Equals(lastFold));

                var obj = JsonConvert.DeserializeObject(File.ReadAllText(attPaths[i]), t);
                if (!(obj is AttributeBase))
                {
                    CoreSystem.Logger.LogWarning(Channel.Creature, $"Attribute({t?.Name}) at {attPaths[i]} is invalid. This attribute has been ignored");
                    continue;
                }
                var temp = (AttributeBase)obj;
                m_Attributes.Add(temp);
            }
        }
        public void SaveData()
        {
            if (!Directory.Exists(CoreSystemFolder.EntityPath)) Directory.CreateDirectory(CoreSystemFolder.EntityPath);
            if (!Directory.Exists(CoreSystemFolder.AttributePath)) Directory.CreateDirectory(CoreSystemFolder.AttributePath);

            if (m_Entites != null)
            {
                string[] entityPaths = Directory.GetFiles(CoreSystemFolder.EntityPath, jsonPostfix, SearchOption.AllDirectories);
                for (int i = 0; i < entityPaths.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(entityPaths[i]);
                    if (m_Entites.Where((other) => other.Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(entityPaths[i]);
                    }
                }
                for (int i = 0; i < m_Entites.Count; i++)
                {
                    string entityPath = Path.Combine(CoreSystemFolder.EntityPath, m_Entites[i].GetType().Name);
                    if (!Directory.Exists(entityPath)) Directory.CreateDirectory(entityPath);

                    if (m_Entites[i].Hash.Equals(Hash.Empty)) m_Entites[i].Hash = Hash.NewHash();

                    File.WriteAllText($"{entityPath}/{m_Entites[i].Name}{json}",
                        JsonConvert.SerializeObject(m_Entites[i], Formatting.Indented));
                }
            }

            if (m_Attributes != null)
            {
                string[] atts = Directory.GetFiles(CoreSystemFolder.AttributePath, jsonPostfix, SearchOption.AllDirectories);
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
                    string attPath = Path.Combine(CoreSystemFolder.AttributePath, m_Attributes[i].GetType().Name);
                    if (!Directory.Exists(attPath)) Directory.CreateDirectory(attPath);

                    if (m_Attributes[i].Hash.Equals(Hash.Empty)) m_Attributes[i].Hash = Hash.NewHash();

                    File.WriteAllText($"{attPath}/{m_Attributes[i].Name}{json}",
                        JsonConvert.SerializeObject(m_Attributes[i], Formatting.Indented));
                }
            }
        }

        public EntityBase GetEntity(Hash hash) => m_Entites.FindFor((other) => other.Hash.Equals(hash));
        public EntityBase GetEntity(string name) => m_Entites.FindFor((other) => other.Name.Equals(name));
        public AttributeBase GetAttribute(Hash hash) => m_Attributes.FindFor((other) => other.Hash.Equals(hash));
    }
}
