using Syadeu.Mono.XNode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono.Creature
{
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "CoreSystem/Creature/Behavior Tree")]
    [RequireNode(typeof(CreatureEntryPointNode))]
#endif
    public class CreatureBehaviorTree : CoreSystemNodeGraphEntity
    {
#if UNITY_EDITOR
        //[ContextMenu("General/Entry Point", false, 1)]
        //public void AddEntryPointNode()
        //{
        //    AddNode<CreatureEntryPointNode>();
        //}


#endif
        private CreatureEntryPointNode EntryPoint => nodes[0] as CreatureEntryPointNode;


        public void Initialize(CreatureBrain brain)
        {
            EntryPoint.m_CreatureBrain = brain;
        }
    }
}