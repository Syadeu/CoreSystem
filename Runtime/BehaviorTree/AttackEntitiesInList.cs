using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.TurnTable;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.BehaviorTree
{
    [TaskCategory("Entity/Actor")]
    public sealed class AttackEntitiesInList : Action
    {
        [SerializeField] private LoadActorControllerAttribute m_ThisActorController;
        [SerializeField] private GetEntitiesActionBase m_InAttackRange;

        [Space]
        [SerializeField] private int m_MaxAttackCount;

        private struct Sort : IComparer<Entity<ActorEntity>>
        {
            public Entity<ActorEntity> m_This;

            public int Compare(Entity<ActorEntity> x, Entity<ActorEntity> y)
            {
                float3
                    thisPos = m_This.transform.position,
                    a = x.transform.position - thisPos,
                    b = y.transform.position - thisPos;

                float
                    aSqr = math.mul(a, a),
                    bSqr = math.mul(b, b);

                if (aSqr < bSqr) return 1;
                else if (aSqr.Equals(bSqr)) return 0;

                return -1;
            }
        }
        public override TaskStatus OnUpdate()
        {
            if (m_InAttackRange.Result.Count == 0) return TaskStatus.Failure;

            Entity<ActorEntity> me = m_ThisActorController.ActorController.Parent.As<IEntityData, ActorEntity>();

            List<Entity<ActorEntity>> temp = m_InAttackRange.Result.Select(Selector).ToList();
            temp.Sort(new Sort() { m_This = m_ThisActorController.ActorController.Parent.As<IEntityData, ActorEntity>() });

            for (int i = 0; i < temp.Count && i < m_MaxAttackCount; i++)
            {
                me.Attack(temp[i]);
            }
            
            return TaskStatus.Success;
        }
        private Entity<ActorEntity> Selector(Entity<IEntity> entity)
        {
            return entity.Cast<IEntity, ActorEntity>();
        }
    }
}
