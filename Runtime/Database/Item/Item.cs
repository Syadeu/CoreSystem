using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        private const string c_DataPath = "/Syadeu/ItemData/";
        private const string c_FileExtension = ".json";
        
        public static string DataPath => Application.persistentDataPath + c_DataPath;
        internal static string EditorDataPath => $"{Application.dataPath}/../{c_DataPath}";

        private Dictionary<int, Item> m_Items = new Dictionary<int, Item>();

        public override void OnInitialize()
        {
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
        public string m_Idx;

        public string m_Name;
        /// <summary>
        /// <see cref="ItemType"/>
        /// </summary>
        public int m_ItemType;
        /// <summary>
        /// <see cref="ItemEffectType"/>
        /// </summary>
        public int[] m_ItemEffectTypes;
        public ItemAffect m_ItemAffects;
    }

    [Serializable]
    public class ItemType
    {
        public string m_Name;
        public int m_Idx;

        public bool m_IsWearable;
        public bool m_IsUsable;
    }

    [Serializable]
    public class ItemEffectType
    {
        public string m_Name;
        public int m_Idx;

        public ItemMath m_Math;
        public float m_Value;
    }
}
