﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu/Item")]
    public sealed class ItemDataList : StaticSettingEntity<ItemDataList>
    {
        private const string c_ItemDataPath = "../CoreSystem/Modules/Items";
        private const string c_ItemTypeDataPath = "../CoreSystem/Modules/Items/ItemTypes";
        private const string c_ItemEffectDataPath = "../CoreSystem/Modules/Items/ItemEffects";

        private ValuePairJsonConverter m_ItemJsonConverter;

        public List<Item> m_Items;
        public List<ItemTypeEntity> m_ItemTypes;
        public List<ItemEffectType> m_ItemEffectTypes;

        public override void OnInitialize()
        {
            LoadDatas();
        }

        private static string GetPath(string dataPath) => $"{Application.dataPath}/{dataPath}";

        #region Data Works

        public void LoadDatas()
        {
            const string jsonPostfix = "*.json";
            if (!Directory.Exists(GetPath(c_ItemDataPath))) Directory.CreateDirectory(GetPath(c_ItemDataPath));
            if (!Directory.Exists(GetPath(c_ItemTypeDataPath))) Directory.CreateDirectory(GetPath(c_ItemTypeDataPath));
            if (!Directory.Exists(GetPath(c_ItemEffectDataPath))) Directory.CreateDirectory(GetPath(c_ItemEffectDataPath));

            string[] dataPaths = Directory.GetFiles(GetPath(c_ItemDataPath), jsonPostfix, SearchOption.TopDirectoryOnly);
            m_Items = new List<Item>();
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_Items.Add(JsonConvert.DeserializeObject<Item>(File.ReadAllText(dataPaths[i])));
            }
            
            dataPaths = Directory.GetFiles(GetPath(c_ItemTypeDataPath), jsonPostfix, SearchOption.TopDirectoryOnly);
            m_ItemTypes = new List<ItemTypeEntity>();
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_ItemTypes.Add(JsonConvert.DeserializeObject<ItemTypeEntity>(File.ReadAllText(dataPaths[i])));
            }
            
            dataPaths = Directory.GetFiles(GetPath(c_ItemEffectDataPath), jsonPostfix, SearchOption.TopDirectoryOnly);
            m_ItemEffectTypes = new List<ItemEffectType>();
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_ItemEffectTypes.Add(JsonConvert.DeserializeObject<ItemEffectType>(File.ReadAllText(dataPaths[i])));
            }
        }
        public void SaveDatas()
        {
            const string json = ".json";
            if (!Directory.Exists(GetPath(c_ItemDataPath))) Directory.CreateDirectory(GetPath(c_ItemDataPath));
            if (!Directory.Exists(GetPath(c_ItemTypeDataPath))) Directory.CreateDirectory(GetPath(c_ItemTypeDataPath));
            if (!Directory.Exists(GetPath(c_ItemEffectDataPath))) Directory.CreateDirectory(GetPath(c_ItemEffectDataPath));

            string[] files;
            if (m_Items != null)
            {
                files = Directory.GetFiles(GetPath(c_ItemDataPath));
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    if (m_Items.Where((other) => other.m_Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(files[i]);
                    }
                }
                for (int i = 0; i < m_Items.Count; i++)
                {
                    if (string.IsNullOrEmpty(m_Items[i].m_Guid))
                    {
                        m_Items[i].m_Guid = Guid.NewGuid().ToString();
                    }

                    File.WriteAllText($"{GetPath(c_ItemDataPath)}/{m_Items[i].m_Name}{json}",
                        JsonConvert.SerializeObject(m_Items[i], Formatting.Indented));
                }
            }
            
            if (m_ItemTypes != null)
            {
                files = Directory.GetFiles(GetPath(c_ItemTypeDataPath));
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    if (m_ItemTypes.Where((other) => other.m_Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(files[i]);
                    }
                }
                for (int i = 0; i < m_ItemTypes.Count; i++)
                {
                    if (string.IsNullOrEmpty(m_ItemTypes[i].m_Guid))
                    {
                        m_ItemTypes[i].m_Guid = Guid.NewGuid().ToString();
                    }

                    File.WriteAllText($"{GetPath(c_ItemTypeDataPath)}/{m_ItemTypes[i].m_Name}{json}",
                        JsonConvert.SerializeObject(m_ItemTypes[i], Formatting.Indented));
                }
            }
            
            if (m_ItemEffectTypes != null)
            {
                files = Directory.GetFiles(GetPath(c_ItemEffectDataPath));
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    if (m_ItemEffectTypes.Where((other) => other.m_Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(files[i]);
                    }
                }
                for (int i = 0; i < m_ItemEffectTypes.Count; i++)
                {
                    if (string.IsNullOrEmpty(m_ItemEffectTypes[i].m_Guid))
                    {
                        m_ItemEffectTypes[i].m_Guid = Guid.NewGuid().ToString();
                    }

                    File.WriteAllText($"{GetPath(c_ItemEffectDataPath)}/{m_ItemEffectTypes[i].m_Name}{json}",
                        JsonConvert.SerializeObject(m_ItemEffectTypes[i], Formatting.Indented));
                }
            }
        }

        #endregion

        #region Gets

        public Item GetItem(string guid)
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].m_Guid.Equals(guid))
                {
                    return m_Items[i];
                }
            }
            return null;
        }
        public Item GetItemByName(string name)
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].m_Name.Equals(name))
                {
                    return m_Items[i];
                }
            }
            return null;
        }
        public ItemTypeEntity GetItemType(string guid)
        {
            for (int i = 0; i < m_ItemTypes.Count; i++)
            {
                if (m_ItemTypes[i].m_Guid.Equals(guid))
                {
                    return m_ItemTypes[i];
                }
            }
            return null;
        }
        public ItemTypeEntity GetItemTypeByName(string name)
        {
            for (int i = 0; i < m_ItemTypes.Count; i++)
            {
                if (m_ItemTypes[i].m_Name.Equals(name))
                {
                    return m_ItemTypes[i];
                }
            }
            return null;
        }
        public ItemEffectType GetItemEffectType(string guid)
        {
            for (int i = 0; i < m_ItemEffectTypes.Count; i++)
            {
                if (m_ItemEffectTypes[i].m_Guid.Equals(guid))
                {
                    return m_ItemEffectTypes[i];
                }
            }
            return null;
        }
        public ItemEffectType GetItemEffectTypeByName(string name)
        {
            for (int i = 0; i < m_ItemEffectTypes.Count; i++)
            {
                if (m_ItemEffectTypes[i].m_Name.Equals(name))
                {
                    return m_ItemEffectTypes[i];
                }
            }
            return null;
        }

        #endregion
    }
}
