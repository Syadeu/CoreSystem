#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using BehaviorDesigner.Runtime.Tasks;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Presentation.Actor;
using Syadeu.Presentation.TurnTable;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace Syadeu.Presentation.BehaviorTree
{
#if CORESYSTEM_TURNBASESYSTEM
    [TaskCategory("Entity/Actor/TRPG")]
    [TaskDescription(
        "AttackOptions 값에 따라 찾은 타겟을 공격합니다." +
        "FindTargetsAction 이 이전에 먼저 수행되어 타겟을 찾아야됩니다.")]
    public sealed class AttackTargetAction : ActionBase
    {
        public enum AttackOptions
        {
            Closest,
            Distant,
            Middle
        }

        [UnityEngine.SerializeField] private AttackOptions m_AttackOptions = AttackOptions.Closest;

        public override TaskStatus OnUpdate()
        {
#if DEBUG_MODE
            if (!Entity.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Invalid Target at {nameof(FindTargetsAction)}.");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<ActorControllerComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doeesn\'t have {nameof(ActorControllerAttribute)}");
                return TaskStatus.Failure;
            }
            else if (!Entity.HasComponent<TRPGActorAttackComponent>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doeesn\'t have {nameof(TRPGActorAttackComponent)}");
                return TaskStatus.Failure;
            }
#endif

            var att = Entity.GetComponent<TRPGActorAttackComponent>();
            if (att.m_Targets.Length == 0) return TaskStatus.Failure;

            var ctr = Entity.GetComponent<ActorControllerComponent>();
#if DEBUG_MODE
            if (!ctr.HasProvider<TRPGActorAttackProvider>())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Entity({Entity.RawName}) doeesn\'t have {nameof(TRPGActorAttackProvider)}");
                return TaskStatus.Failure;
            }
#endif
            var attProvider = ctr.GetProvider<TRPGActorAttackProvider>();

            Comparer comparer = new Comparer(Entity.transform.position);
            var iter = att.m_Targets.OrderBy(Order, comparer);

            switch (m_AttackOptions)
            {
                default:
                case AttackOptions.Closest:
                    attProvider.GetObject().Attack(iter.First().GetEntity<ActorEntity>());
                    break;
                case AttackOptions.Distant:
                    attProvider.GetObject().Attack(iter.Last().GetEntity<ActorEntity>());
                    break;
                case AttackOptions.Middle:
                    int index = iter.Count() / 2;
                    attProvider.GetObject().Attack(iter.ElementAt(index).GetEntity<ActorEntity>());
                    break;
            }

            return TaskStatus.Success;
        }

        private static ITransform Order(EntityID id)
        {
            return id.GetEntity<IEntity>().transform;
        }
        private struct Comparer : IComparer<ITransform>
        {
            public float3 myPos;

            public Comparer(float3 center)
            {
                myPos = center;
            }
            public int Compare(ITransform x, ITransform y)
            {
                float3
                    tempX = x.position - myPos,
                    tempY = y.position - myPos;
                float
                    xMag = math.dot(tempX, tempX),
                    yMag = math.dot(tempY, tempY);

                if (xMag < yMag) return -1;
                else if (xMag == yMag) return 0;
                return 1;
            }
        }
    }
#endif
}
