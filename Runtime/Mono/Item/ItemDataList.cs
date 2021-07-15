using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Syadeu.Database
{
    [PreferBinarySerialization][CustomStaticSetting("Syadeu/Item")]
    public sealed class ItemDataList : StaticSettingEntity<ItemDataList>
    {
        public List<Item> m_Items;
        public List<ItemTypeEntity> m_ItemTypes;
        public List<ItemEffectType> m_ItemEffectTypes;

        public override void OnInitialize()
        {
            LoadData();
        }

        #region Data Works

        public void LoadData()
        {
            const string jsonPostfix = "*.json";
            if (!Directory.Exists(CoreSystemFolder.ItemPath)) Directory.CreateDirectory(CoreSystemFolder.ItemPath);
            if (!Directory.Exists(CoreSystemFolder.ItemTypePath)) Directory.CreateDirectory(CoreSystemFolder.ItemTypePath);
            if (!Directory.Exists(CoreSystemFolder.ItemEffectTypePath)) Directory.CreateDirectory(CoreSystemFolder.ItemEffectTypePath);

            string[] dataPaths = Directory.GetFiles(CoreSystemFolder.ItemPath, jsonPostfix, SearchOption.TopDirectoryOnly);
            m_Items = new List<Item>();
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_Items.Add(JsonConvert.DeserializeObject<Item>(File.ReadAllText(dataPaths[i])));
            }
            
            dataPaths = Directory.GetFiles(CoreSystemFolder.ItemTypePath, jsonPostfix, SearchOption.TopDirectoryOnly);
            m_ItemTypes = new List<ItemTypeEntity>();
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_ItemTypes.Add(JsonConvert.DeserializeObject<ItemTypeEntity>(File.ReadAllText(dataPaths[i])));
            }
            
            dataPaths = Directory.GetFiles(CoreSystemFolder.ItemEffectTypePath, jsonPostfix, SearchOption.TopDirectoryOnly);
            m_ItemEffectTypes = new List<ItemEffectType>();
            for (int i = 0; i < dataPaths.Length; i++)
            {
                m_ItemEffectTypes.Add(JsonConvert.DeserializeObject<ItemEffectType>(File.ReadAllText(dataPaths[i])));
            }
        }
        public void SaveData()
        {
            const string json = ".json";
            if (!Directory.Exists(CoreSystemFolder.ItemPath)) Directory.CreateDirectory(CoreSystemFolder.ItemPath);
            if (!Directory.Exists(CoreSystemFolder.ItemTypePath)) Directory.CreateDirectory(CoreSystemFolder.ItemTypePath);
            if (!Directory.Exists(CoreSystemFolder.ItemEffectTypePath)) Directory.CreateDirectory(CoreSystemFolder.ItemEffectTypePath);

            string[] files;
            if (m_Items != null)
            {
                files = Directory.GetFiles(CoreSystemFolder.ItemPath);
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    if (m_Items.Where((other) => other.Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(files[i]);
                    }
                }
                for (int i = 0; i < m_Items.Count; i++)
                {
                    if (Hash.Empty.Equals(m_Items[i].Hash))
                    {
                        m_Items[i].Hash = Hash.NewHash();
                    }

                    File.WriteAllText($"{CoreSystemFolder.ItemPath}/{m_Items[i].Name}{json}",
                        JsonConvert.SerializeObject(m_Items[i], Formatting.Indented));
                }
            }
            
            if (m_ItemTypes != null)
            {
                files = Directory.GetFiles(CoreSystemFolder.ItemTypePath);
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    if (m_ItemTypes.Where((other) => other.Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(files[i]);
                    }
                }
                for (int i = 0; i < m_ItemTypes.Count; i++)
                {
                    if (Hash.Empty.Equals(m_ItemTypes[i].Hash))
                    {
                        m_ItemTypes[i].Hash = Hash.NewHash();
                    }

                    File.WriteAllText($"{CoreSystemFolder.ItemTypePath}/{m_ItemTypes[i].Name}{json}",
                        JsonConvert.SerializeObject(m_ItemTypes[i], Formatting.Indented));
                }
            }
            
            if (m_ItemEffectTypes != null)
            {
                files = Directory.GetFiles(CoreSystemFolder.ItemEffectTypePath);
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    if (m_ItemEffectTypes.Where((other) => other.Name.Equals(fileName)).Count() == 0)
                    {
                        File.Delete(files[i]);
                    }
                }
                for (int i = 0; i < m_ItemEffectTypes.Count; i++)
                {
                    if (Hash.Empty.Equals(m_ItemEffectTypes[i].Hash))
                    {
                        m_ItemEffectTypes[i].Hash = Hash.NewHash();
                    }

                    File.WriteAllText($"{CoreSystemFolder.ItemEffectTypePath}/{m_ItemEffectTypes[i].Name}{json}",
                        JsonConvert.SerializeObject(m_ItemEffectTypes[i], Formatting.Indented));
                }
            }
        }

        #endregion

        #region Gets

        public Item GetItem(Hash hash)
        {
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].Hash.Equals(hash))
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
                if (m_Items[i].Name.Equals(name))
                {
                    return m_Items[i];
                }
            }
            return null;
        }
        public ItemTypeEntity GetItemType(Hash hash)
        {
            for (int i = 0; i < m_ItemTypes.Count; i++)
            {
                if (m_ItemTypes[i].Hash.Equals(hash))
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
                if (m_ItemTypes[i].Name.Equals(name))
                {
                    return m_ItemTypes[i];
                }
            }
            return null;
        }
        public ItemEffectType GetItemEffectType(Hash hash)
        {
            for (int i = 0; i < m_ItemEffectTypes.Count; i++)
            {
                if (m_ItemEffectTypes[i].Hash.Equals(hash))
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
                if (m_ItemEffectTypes[i].Name.Equals(name))
                {
                    return m_ItemEffectTypes[i];
                }
            }
            return null;
        }

        #endregion
    }
}
