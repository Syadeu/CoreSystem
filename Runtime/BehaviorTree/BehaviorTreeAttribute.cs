using UnityEngine;
using Syadeu.Presentation.Attributes;
using Syadeu.Collections;
using BehaviorDesigner.Runtime;
using Newtonsoft.Json;
using Syadeu.Presentation.Entities;
using UnityEngine.ResourceManagement.AsyncOperations;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.TurnTable;
using Syadeu.Presentation.Actor;
using System.ComponentModel;

namespace Syadeu.Presentation.BehaviorTree
{
    [AttributeAcceptOnly(typeof(ActorEntity))]
    [DisplayName("Attribute: Behavior Tree")]
    public sealed class BehaviorTreeAttribute : AttributeBase
    {
        [JsonProperty(Order = 0, PropertyName = "BehaviorTree")] 
        private PrefabReference<ExternalBehavior> m_BehaviorTree = PrefabReference<ExternalBehavior>.None;

        [Space, Header("Turn Player")]
        [JsonProperty(Order = 1, PropertyName = "StartOnTurn")] public bool m_StartOnTurn = false;

        [JsonIgnore] private ExternalBehavior m_InstanceBehaviorTree;

        [JsonIgnore] public PrefabReference<ExternalBehavior> BehaviorTree => m_BehaviorTree;
        [JsonIgnore] public ExternalBehavior InstanceBehaviorTree => m_InstanceBehaviorTree;
        [JsonIgnore] public Behavior BehaviorTreeComponent
        {
            get
            {
                Entity<IEntity> entity = Parent.ToEntity<IEntity>();
                if (!entity.hasProxy)
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Entity({entity.Name}) has no proxy.");
                    return null;
                }
                UnityEngine.Object obj = entity.proxy;
                if (!(obj is RecycleableMonobehaviour monoObj))
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Target entity({entity.Name}) is not a Entity.");
                    return null;
                }
                return monoObj.GetComponent<Behavior>();
            }
        }

        public void LoadBehaviorTreeAsync(AsyncOperationHandle<ExternalBehavior> obj) => LoadBehaviorTree(obj.Result);
        public void LoadBehaviorTree(ExternalBehavior obj)
        {
            m_InstanceBehaviorTree = UnityEngine.Object.Instantiate(obj);
            m_InstanceBehaviorTree.Init();
        }
        public void DestroyBehaviorTree()
        {
            UnityEngine.Object.Destroy(m_InstanceBehaviorTree);
            m_InstanceBehaviorTree = null;
        }

        public void StartBehaviorTree()
        {
            Behavior behavior = BehaviorTreeComponent;
            if (behavior == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"Unknown error: {Parent.Name} behavior tree null.");
                return;
            }

            behavior.DisableBehavior();
            behavior.EnableBehavior();
        }
    }
    internal sealed class BehaviorTreeProcessor : AttributeProcessor<BehaviorTreeAttribute>,
        IAttributeOnProxy
    {
        protected override void OnInitialize()
        {
            EventSystem.AddEvent<OnTurnStateChangedEvent>(OnTurnStateChangedEventHandler);
        }
        protected override void OnDispose()
        {
            EventSystem.RemoveEvent<OnTurnStateChangedEvent>(OnTurnStateChangedEventHandler);
        }
        private void OnTurnStateChangedEventHandler(OnTurnStateChangedEvent ev)
        {
            BehaviorTreeAttribute behavior = ev.Entity.GetAttribute<BehaviorTreeAttribute>();
            if (behavior == null || !behavior.m_StartOnTurn) return;

            if ((ev.State & OnTurnStateChangedEvent.TurnState.Start) == OnTurnStateChangedEvent.TurnState.Start)
            {
                behavior.StartBehaviorTree();
            }
        }

        protected override void OnCreated(BehaviorTreeAttribute attribute, Entity<IEntityData> entity)
        {
            if (attribute.BehaviorTree.IsNone() || !attribute.BehaviorTree.IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) has {nameof(BehaviorTreeAttribute)} but attribute has an invalid behavior tree. This is not allowed.");
                return;
            }

            if (attribute.BehaviorTree.Asset == null)
            {
                AsyncOperationHandle<ExternalBehavior> temp = attribute.BehaviorTree.LoadAssetAsync();
                temp.Completed += attribute.LoadBehaviorTreeAsync;
            }
            else attribute.LoadBehaviorTree(attribute.BehaviorTree.Asset);

            if (entity.Target is IEntity)
            {
                var tr = entity.ToEntity<IEntity>().transform;
                if (tr is ProxyTransform proxy)
                {
                    proxy.enableCull = false;
                }
            }
        }
        protected override void OnDestroy(BehaviorTreeAttribute attribute, Entity<IEntityData> entity)
        {
            attribute.DestroyBehaviorTree();
        }

        public void OnProxyCreated(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            BehaviorTreeAttribute att = (BehaviorTreeAttribute)attribute;
            Behavior behavior = monoObj.GetComponent<Behavior>();
            if (behavior == null)
            {
                behavior = monoObj.AddComponent<InternalBehaviorTree>();
                behavior.StartWhenEnabled = false;
                behavior.DisableBehavior();
            }

            SharedEntity sharedEntity = entity;

            behavior.ExternalBehavior = att.InstanceBehaviorTree;
            behavior.SetVariable(PresentationBehaviorTreeUtility.c_SelfEntityString, sharedEntity);
        }
        public void OnProxyRemoved(IAttribute attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj)
        {
            Behavior behavior = monoObj.GetComponent<Behavior>();

            behavior.DisableBehavior();

            behavior.ExternalBehavior = null;
        }
    }
}
