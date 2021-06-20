using Syadeu.Database;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class CreatureInventory : CreatureEntity
    {
        [Space]
        [SerializeField] private List<int> m_Equipments = new List<int>();
        [SerializeField] private List<int> m_Inventory = new List<int>();
    }
}
