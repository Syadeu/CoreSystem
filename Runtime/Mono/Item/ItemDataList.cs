﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu/Item")]
    public sealed class ItemDataList : StaticSettingEntity<ItemDataList>
    {
        public const string c_ItemDataPath = "../CoreSystem/Modules/Items";
        public const string c_ItemTypeDataPath = "../CoreSystem/Modules/Items/ItemTypes";
        public const string c_ItemEffectDataPath = "../CoreSystem/Modules/Items/ItemEffects";

        public Item[] m_Items;
        public ItemType[] m_ItemTypes;
        public ItemEffectType[] m_ItemEffectTypes;

        public override bool RuntimeModifiable => true;

        public override void OnInitialize()
        {
            
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            for (int i = 0; i < m_Items?.Length; i++)
            {
                if (string.IsNullOrEmpty(m_Items[i].m_Guid))
                {
                    m_Items[i].m_Guid = Guid.NewGuid().ToString();
                }
            }
            for (int i = 0; i < m_ItemTypes?.Length; i++)
            {
                if (string.IsNullOrEmpty(m_ItemTypes[i].m_Guid))
                {
                    m_ItemTypes[i].m_Guid = Guid.NewGuid().ToString();
                }
            }
            for (int i = 0; i < m_ItemEffectTypes?.Length; i++)
            {
                if (string.IsNullOrEmpty(m_ItemEffectTypes[i].m_Guid))
                {
                    m_ItemEffectTypes[i].m_Guid = Guid.NewGuid().ToString();
                }
            }
        }
#endif
        private string GetPath(string dataPath) => $"{Application.dataPath}/{dataPath}";

        public void LoadDatas()
        {
            const string jsonPostfix = "*.json";
            if (!Directory.Exists(GetPath(c_ItemDataPath))) Directory.CreateDirectory(GetPath(c_ItemDataPath));
            if (!Directory.Exists(GetPath(c_ItemTypeDataPath))) Directory.CreateDirectory(GetPath(c_ItemTypeDataPath));
            if (!Directory.Exists(GetPath(c_ItemEffectDataPath))) Directory.CreateDirectory(GetPath(c_ItemEffectDataPath));

            string[] dataPaths = Directory.GetFiles(GetPath(c_ItemDataPath), jsonPostfix, SearchOption.TopDirectoryOnly);
            m_Items = new Item[dataPaths.Length];
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_Items[i] = JsonConvert.DeserializeObject<Item>(File.ReadAllText(dataPaths[i]));
            }
            
            dataPaths = Directory.GetFiles(GetPath(c_ItemTypeDataPath), jsonPostfix, SearchOption.TopDirectoryOnly);
            m_ItemTypes = new ItemType[dataPaths.Length];
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_ItemTypes[i] = JsonConvert.DeserializeObject<ItemType>(File.ReadAllText(dataPaths[i]));
            }
            
            dataPaths = Directory.GetFiles(GetPath(c_ItemEffectDataPath), jsonPostfix, SearchOption.TopDirectoryOnly);
            m_ItemEffectTypes = new ItemEffectType[dataPaths.Length];
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_ItemEffectTypes[i] = JsonConvert.DeserializeObject<ItemEffectType>(File.ReadAllText(dataPaths[i]));
            }
        }
        public void SaveDatas()
        {
            const string json = ".json";
            if (!Directory.Exists(GetPath(c_ItemDataPath))) Directory.CreateDirectory(GetPath(c_ItemDataPath));
            if (!Directory.Exists(GetPath(c_ItemTypeDataPath))) Directory.CreateDirectory(GetPath(c_ItemTypeDataPath));
            if (!Directory.Exists(GetPath(c_ItemEffectDataPath))) Directory.CreateDirectory(GetPath(c_ItemEffectDataPath));

            for (int i = 0; i < m_Items?.Length; i++)
            {
                File.WriteAllText($"{GetPath(c_ItemDataPath)}/{m_Items[i].m_Name}{json}", 
                    JsonConvert.SerializeObject(m_Items[i], Formatting.Indented));
            }
            for (int i = 0; i < m_ItemTypes?.Length; i++)
            {
                File.WriteAllText($"{GetPath(c_ItemTypeDataPath)}/{m_ItemTypes[i].m_Name}{json}", 
                    JsonConvert.SerializeObject(m_ItemTypes[i], Formatting.Indented));
            }
            for (int i = 0; i < m_ItemEffectTypes?.Length; i++)
            {
                File.WriteAllText($"{GetPath(c_ItemEffectDataPath)}/{m_ItemEffectTypes[i].m_Name}{json}", 
                    JsonConvert.SerializeObject(m_ItemEffectTypes[i], Formatting.Indented));
            }
        }

        public Item GetItem(string guid)
        {
            for (int i = 0; i < m_Items.Length; i++)
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
            for (int i = 0; i < m_Items.Length; i++)
            {
                if (m_Items[i].m_Name.Equals(name))
                {
                    return m_Items[i];
                }
            }
            return null;
        }
        public ItemType GetItemType(string guid)
        {
            for (int i = 0; i < m_ItemTypes.Length; i++)
            {
                if (m_ItemTypes[i].m_Guid.Equals(guid))
                {
                    return m_ItemTypes[i];
                }
            }
            return null;
        }
        public ItemEffectType GetItemEffectType(string guid)
        {
            for (int i = 0; i < m_ItemEffectTypes.Length; i++)
            {
                if (m_ItemEffectTypes[i].m_Guid.Equals(guid))
                {
                    return m_ItemEffectTypes[i];
                }
            }
            return null;
        }
    }
}
