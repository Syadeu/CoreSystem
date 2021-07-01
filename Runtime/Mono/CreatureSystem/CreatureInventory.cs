using Syadeu.Database;
using System;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Syadeu.Mono
{
    public sealed class CreatureInventory : CreatureEntity
    {
        [Space]
        [SerializeField] private List<ItemInstance> m_Equipments = new List<ItemInstance>();
        [SerializeField] private List<ItemInstance> m_Inventory = new List<ItemInstance>();

        public IReadOnlyList<ItemInstance> Inventory => m_Inventory;

        protected override void OnCreated()
        {
            ValidateData();
        }

        private void ValidateData()
        {
            for (int i = m_Equipments.Count - 1; i >= 0; i--)
            {
                if (!m_Equipments[i].IsValid())
                {
                    m_Equipments.RemoveAt(i);
                }
            }
            for (int i = m_Inventory.Count - 1; i >= 0; i--)
            {
                if (!m_Inventory[i].IsValid())
                {
                    m_Inventory.RemoveAt(i);
                }
            }
        }

        public void Insert(ItemInstance item)
        {
            m_Inventory.Add(item);
        }
        public void Use(int i)
        {
            ItemInstance item = m_Inventory[i];

            if (item.HasType<ItemUseableType>())
            {
                ItemUseableType useableType = item.GetType<ItemUseableType>();
                for (int j = 0; j < useableType.m_OnUse.Count; j++)
                {
                    if (useableType.m_OnUse[j] is SerializableClosureValuePair closure)
                    {
                        closure.Invoke(Brain.Proxy);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }

                if (useableType.m_RemoveOnUse)
                {
                    m_Inventory[i].Dispose();
                    m_Inventory.RemoveAt(i);
                }
            }
            else
            {
                $"Item: {item.Data.m_Name} is not a type of useable".ToLog();
            }
        }
    }
}
