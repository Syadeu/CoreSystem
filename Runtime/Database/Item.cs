﻿using System;
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
