using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization] [CustomStaticSetting("Syadeu")]
    public sealed class EntityDataList : StaticSettingEntity<EntityDataList>
    {
        const string jsonPostfix = "*.json";
        const string json = ".json";

        public Dictionary<Hash, ObjectBase> m_Objects;
        private Dictionary<string, Hash> m_EntityHash;

        private void OnEnable()
        {
            LoadData();
        }

        public void Purge()
        {
            m_Objects.Clear();
            m_EntityHash.Clear();
        }
        public void LoadData()
        {
            if (!Directory.Exists(CoreSystemFolder.EntityPath)) Directory.CreateDirectory(CoreSystemFolder.EntityPath);
            if (!Directory.Exists(CoreSystemFolder.AttributePath)) Directory.CreateDirectory(CoreSystemFolder.AttributePath);

            m_Objects = new Dictionary<Hash, ObjectBase>();
            m_EntityHash = new Dictionary<string, Hash>();

            string[] entityPaths = Directory.GetFiles(CoreSystemFolder.EntityPath, jsonPostfix, SearchOption.AllDirectories);
            //m_Entites = new List<EntityBase>();
            Type[] entityTypes = TypeHelper.GetTypes(
                (other) => TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(other));
            for (int i = 0; i < entityPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(entityPaths[i]));
                Type t = entityTypes.FindFor((other) => other.Name.Equals(lastFold));

                var obj = JsonConvert.DeserializeObject(File.ReadAllText(entityPaths[i]), t);
                if (!(obj is ObjectBase))
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Entity({t?.Name}) at {entityPaths[i]} is invalid. This entity has been ignored");
                    continue;
                }

                var temp = (ObjectBase)obj;
                m_Objects.Add(temp.Hash, temp);
                m_EntityHash.Add(temp.Name, temp.Hash);
                //m_Entites.Add(temp);
            }

            string[] attPaths = Directory.GetFiles(CoreSystemFolder.AttributePath, jsonPostfix, SearchOption.AllDirectories);
            //m_Attributes = new List<AttributeBase>();
            Type[] attTypes = TypeHelper.GetTypes((other) => TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(other));
            for (int i = 0; i < attPaths.Length; i++)
            {
                string lastFold = Path.GetFileName(Path.GetDirectoryName(attPaths[i]));
                Type t = attTypes.FindFor((other) => other.Name.Equals(lastFold));

                object obj;
                try
                {
                    obj = JsonConvert.DeserializeObject(File.ReadAllText(attPaths[i]), t);
                    if (!(obj is AttributeBase))
                    {
                        CoreSystem.Logger.LogWarning(Channel.Entity, $"Attribute({t?.Name}) at {attPaths[i]} is invalid. This attribute has been ignored");
                        continue;
                    }
                }
                catch (Exception)
                {
                    CoreSystem.Logger.LogWarning(Channel.Entity, $"Attribute({t?.Name}) at {attPaths[i]} is invalid. This attribute has been ignored");
                    continue;
                }
                var temp = (AttributeBase)obj;
                m_Objects.Add(temp.Hash, temp);
                //m_ObjectHash.Add(temp.Name, temp.Hash);
                //m_Attributes.Add(temp);
            }
        }
        private void DeleteEmptyFolders()
        {
            if (!Directory.Exists(CoreSystemFolder.EntityPath)) Directory.CreateDirectory(CoreSystemFolder.EntityPath);
            if (!Directory.Exists(CoreSystemFolder.AttributePath)) Directory.CreateDirectory(CoreSystemFolder.AttributePath);

            string[] paths = Directory.GetDirectories(CoreSystemFolder.EntityPath);
            for (int i = 0; i < paths.Length; i++)
            {
                if (Directory.GetFiles(paths[i]).Length > 0) continue;

                Directory.Delete(paths[i]);
            }
            paths = Directory.GetDirectories(CoreSystemFolder.AttributePath);
            for (int i = 0; i < paths.Length; i++)
            {
                if (Directory.GetFiles(paths[i]).Length > 0) continue;

                Directory.Delete(paths[i]);
            }
        }
        public void SaveData(ObjectBase obj)
        {
            if (!Directory.Exists(CoreSystemFolder.EntityPath)) Directory.CreateDirectory(CoreSystemFolder.EntityPath);
            if (!Directory.Exists(CoreSystemFolder.AttributePath)) Directory.CreateDirectory(CoreSystemFolder.AttributePath);

            Type objType = obj.GetType();
            string objPath;
            if (TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(objType))
            {
                objPath = Path.Combine(CoreSystemFolder.EntityPath, objType.Name);
            }
            else objPath = Path.Combine(CoreSystemFolder.AttributePath, objType.Name);

            if (!Directory.Exists(objPath)) Directory.CreateDirectory(objPath);

            if (obj.Hash.Equals(Hash.Empty)) obj.Hash = Hash.NewHash();

            File.WriteAllText($"{objPath}/{obj.Name}{json}",
                JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
        public void SaveData()
        {
            if (!Directory.Exists(CoreSystemFolder.EntityPath)) Directory.CreateDirectory(CoreSystemFolder.EntityPath);
            if (!Directory.Exists(CoreSystemFolder.AttributePath)) Directory.CreateDirectory(CoreSystemFolder.AttributePath);

            ObjectBase[] m_Entites = GetEntities();
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
                for (int i = 0; i < m_Entites.Length; i++)
                {
                    string entityPath = Path.Combine(CoreSystemFolder.EntityPath, m_Entites[i].GetType().Name);
                    if (!Directory.Exists(entityPath)) Directory.CreateDirectory(entityPath);

                    if (m_Entites[i].Hash.Equals(Hash.Empty)) m_Entites[i].Hash = Hash.NewHash();

                    File.WriteAllText($"{entityPath}/{m_Entites[i].Name}{json}",
                        JsonConvert.SerializeObject(m_Entites[i], Formatting.Indented));
                }
            }
            else "nothing to save entit".ToLog();

            AttributeBase[] m_Attributes = GetAttributes();
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

                for (int i = 0; i < m_Attributes.Length; i++)
                {
                    string attPath = Path.Combine(CoreSystemFolder.AttributePath, m_Attributes[i].GetType().Name);
                    if (!Directory.Exists(attPath)) Directory.CreateDirectory(attPath);

                    if (m_Attributes[i].Hash.Equals(Hash.Empty)) m_Attributes[i].Hash = Hash.NewHash();

                    File.WriteAllText($"{attPath}/{m_Attributes[i].Name}{json}",
                        JsonConvert.SerializeObject(m_Attributes[i], Formatting.Indented));
                }
            }
            else "nothing to save att".ToLog();

            DeleteEmptyFolders();
        }

        public EntityDataBase[] GetEntities()
        {
            if (m_Objects == null) return Array.Empty<EntityDataBase>();
            return m_Objects
                    .Where((other) =>
                    {
                        return TypeHelper.TypeOf<EntityDataBase>.Type.IsAssignableFrom(other.Value.GetType());
                    })
                    .Select((other) => (EntityDataBase)other.Value)
                    .ToArray();
        }
        public AttributeBase[] GetAttributes()
        {
            if (m_Objects == null) return Array.Empty<AttributeBase>();
            return m_Objects
                    .Where((other) =>
                    {
                        return TypeHelper.TypeOf<AttributeBase>.Type.IsAssignableFrom(other.Value.GetType());
                    })
                    .Select((other) => (AttributeBase)other.Value)
                    .ToArray();
        }

        public ObjectBase GetObject(Hash hash)
        {
            if (hash.Equals(Hash.Empty)) throw new KeyNotFoundException();
            return m_Objects[hash];
        }
        public ObjectBase GetObject(string name) => GetObject(m_EntityHash[name]);
        //public EntityBase GetEntity(Hash hash) => m_Entites.FindFor((other) => other.Hash.Equals(hash));
        //public EntityBase GetEntity(string name) => m_Entites.FindFor((other) => other.Name.Equals(name));
        //public AttributeBase GetAttribute(Hash hash) => m_Attributes.FindFor((other) => other.Hash.Equals(hash));
    }
}
