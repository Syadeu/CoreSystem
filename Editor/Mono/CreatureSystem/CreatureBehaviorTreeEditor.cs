//using System.Collections;
//using System.Collections.Generic;
//using System.Reflection;
//using System.Linq;
//using UnityEngine;

//using Syadeu.Mono.Creature;

//using XNode;
//using XNodeEditor;
//using System;
//using Syadeu;

//namespace SyadeuEditor
//{
//    [CustomNodeGraphEditor(typeof(CreatureBehaviorTree))]
//    public class CreatureBehaviorTreeEditor : NodeGraphEditor
//    {
//        public override string GetNodeMenuName(Type type)
//        {
//            if (!type.Assembly.Equals(typeof(CoreSystem).Assembly))
//            {
//                return base.GetNodeMenuName(type);
//            }
//            return null;
//        }

//        public override void OnOpen()
//        {
//            var temp = target.nodes
//                .Select((node) =>
//                {
//                    return node.GetType().Equals(typeof(CreatureEntryPointNode));
//                })
//                .ToArray();
//            if (temp == null || temp.Length == 0)
//            {
//                CreateNode(typeof(CreatureEntryPointNode), Vector2.zero);
//            }
                
            
//        }
//    }
//}
