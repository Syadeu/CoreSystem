using Syadeu.Database;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class CreatureInventory : CreatureEntity
    {
        [Space]
        [SerializeField] private List<string> m_Equipments = new List<string>();
        [SerializeField] private List<string> m_Inventory = new List<string>();
    }
}
