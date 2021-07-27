//using UnityEngine;

//using XNode;

//namespace Syadeu.Mono.XNode
//{
//    public abstract class CoreSystemNodeGraphEntity : NodeGraph
//    {
//#if UNITY_EDITOR
//        #region If
//        [ContextMenu("Logic/If/Int", false, 1)]
//        public void AddIfIntNode()
//        {
//            AddNode<IfIntLogic>();
//        }
//        [ContextMenu("Logic/If/Float", false, 1)]
//        public void AddIfFloatNode()
//        {
//            AddNode<IfBoolLogic>();
//        }
//        [ContextMenu("Logic/If/Bool", false, 1)]
//        public void AddIfBoolNode()
//        {
//            AddNode<IfBoolLogic>();
//        }
//        [ContextMenu("Logic/If/String", false, 1)]
//        public void AddIfStringNode()
//        {
//            AddNode<IfBoolLogic>();
//        }
//        #endregion

//        #region WaitFor
//        [ContextMenu("Logic/WaitFor/Int", false, 1)]
//        public void AddWaitForIntNode()
//        {
//            AddNode<WaitForIntLogic>();
//        }
//        [ContextMenu("Logic/WaitFor/Float", false, 1)]
//        public void AddWaitForFloatNode()
//        {
//            AddNode<WaitForFloatLogic>();
//        }
//        [ContextMenu("Logic/WaitFor/Bool", false, 1)]
//        public void AddWaitForBoolNode()
//        {
//            AddNode<WaitForBoolLogic>();
//        }
//        [ContextMenu("Logic/WaitFor/String", false, 1)]
//        public void AddWaitForStringNode()
//        {
//            AddNode<WaitForStringLogic>();
//        }
//        #endregion

//#endif
//    }
//}