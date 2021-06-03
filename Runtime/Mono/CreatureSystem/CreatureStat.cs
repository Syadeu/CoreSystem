using Syadeu.Mono;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Creature
{
    [RequireComponent(typeof(CreatureBrain))]
    public class CreatureStat : CreatureEntity
    {
        [SerializeField] private CreatureStatReference m_StatReference;
    }
}
