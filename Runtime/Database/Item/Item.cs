using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Syadeu.Database
{
    public enum ItemMath
    {
        Plus,
        Minus,

        Multiply,
        Divide,
    }
    public enum ItemAffect
    {
        None = 0,

        HP = 0x01,
        AP = 0x02,

        All = ~0
    }

    [StaticManagerIntializeOnLoad]
    public sealed class ItemDataManager : StaticDataManager<ItemDataManager>
    {
        private Dictionary<int, Item> m_Items = new Dictionary<int, Item>();

        public override void OnInitialize()
        {
            //$"{DataManager.DataPath}/Item".ToLog();
            //Resources.LoadAll<>
        }

        private static void SaveItem(string path, Item item)    
        {
            string data = JsonConvert.SerializeObject(item);
            File.WriteAllText(path + $"Item/{item.m_Name}.json", data);
        }
    }

    [Serializable]
    public sealed class Item
    {
        public string m_Name;
        public string m_Guid;

        /// <summary>
        /// <see cref="ItemType"/>
        /// </summary>
        public string[] m_ItemTypes;
        /// <summary>
        /// <see cref="ItemEffectType"/>
        /// </summary>
        public string[] m_ItemEffectTypes;
        public ItemAffect m_ItemAffects;
    }

    [Serializable]
    public sealed class ItemType
    {
        public string m_Name;
        public string m_Guid;
    }

    [Serializable]
    public sealed class ItemEffectType
    {
        public string m_Name;
        public string m_Guid;

        public ItemMath m_Math;
        public float m_Value;
    }
}
