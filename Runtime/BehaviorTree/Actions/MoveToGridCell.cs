//#if CORESYSTEM_TURNBASESYSTEM

//using BehaviorDesigner.Runtime;
//using BehaviorDesigner.Runtime.Tasks;
//using Syadeu.Presentation.Map;
//using Syadeu.Presentation.TurnTable;
//using Unity.Mathematics;
//using UnityEngine;

//namespace Syadeu.Presentation.BehaviorTree
//{
//    [TaskCategory("Entity/Actor")]
//    public sealed class MoveToGridCell : Action
//    {
//        public enum Source
//        {
//            Index,
//            GetClosestGridCellFromTarget,
//        }


//        [SerializeField] private LoadNavAgentAttribute m_ThisNavAgent;
//        [SerializeField] private LoadGridSizeAttribute m_ThisGridSize;
//        [SerializeField] private LoadTurnPlayerAttribute m_ThisTurnPlayer;

//        [Space]
//        [SerializeField] private Source m_Source;

//        [Space]
//        [SerializeField] private int m_GridCellIndex;
//        [SerializeField] private GetClosestGridCellFromTarget m_GetClosestGridCellFromTarget;


//        public override TaskStatus OnUpdate()
//        {
//            int index;
//            switch (m_Source)
//            {
//                case Source.GetClosestGridCellFromTarget:
//                    index = m_GetClosestGridCellFromTarget.Result;
//                    break;
//                default:
//                    index = m_GridCellIndex;
//                    break;
//            }

//            if (!m_ThisGridSize.GridSizeAttribute.Parent.HasComponent<GridSizeComponent>())
//            {
//                return TaskStatus.Failure;
//            }
//            GridSizeComponent component = m_ThisGridSize.GridSizeAttribute.Parent.GetComponent<GridSizeComponent>();
//            ref TurnPlayerComponent turnPlayer = ref m_ThisGridSize.GridSizeAttribute.Parent.GetComponent<TurnPlayerComponent>();

//            GridPath64 path = GridPath64.Create();
//            component.GetPath64(index, ref path);

//            TRPGActorMoveComponent move = m_ThisGridSize.GridSizeAttribute.Parent.GetComponent<TRPGActorMoveComponent>();
//            move.MoveTo(in path, new ActorMoveEvent(m_ThisGridSize.GridSizeAttribute.Parent, 1));

//            //float3 pos = PresentationSystem<GridSystem>.System.IndexToPosition(index);


//            turnPlayer.ActionPoint -= path.Length;
//            //m_ThisNavAgent.NavAgentAttribute.MoveTo(pos);

//            return TaskStatus.Success;
//        }
//    }
//}

//#endif