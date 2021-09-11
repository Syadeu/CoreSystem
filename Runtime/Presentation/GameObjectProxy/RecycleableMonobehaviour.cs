using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace Syadeu.Presentation.Proxy
{
    [DisallowMultipleComponent]
    /// <summary>
    /// 재사용 가능 오브젝트들의 기본 참조 클래스입니다<br/>
    /// Awake, Start 함수를 절때 사용하지마세요 대신 OnInitialize를 사용하세요
    /// OnDestroy 함수를 절때 사용하지마세요
    /// </summary>
    /// <typeparam name="T"></typeparam>    
    public abstract class RecycleableMonobehaviour : MonoBehaviour, IValidation, INotificationReceiver
    {
        public delegate bool TerminateCondition();
        /// <summary>
        /// <see cref="Presentation.GameObjectProxySystem.m_Instances"/> value 리스트의 인덱스입니다.
        /// </summary>
        internal int m_Idx = -1;

        internal EventSystem m_EventSystem;
        private GameObject m_GameObject;
        private Transform m_Transform;
        internal Entity<IEntity> m_Entity;
        private readonly Dictionary<IComponentID, List<Component>> m_Components = new Dictionary<IComponentID, List<Component>>();

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// <see cref="Presentation.GameObjectProxySystem"/> 에서 파싱한 <see cref="GameObject"/> 입니다.
        /// </summary>
        public new GameObject gameObject => m_GameObject;
        /// <summary>
        /// <see cref="Presentation.GameObjectProxySystem"/> 에서 파싱한 <see cref="Transform"/> 입니다.
        /// </summary>
        public new Transform transform => m_Transform;
        public Entity<IEntity> entity => m_Entity;
        public EventSystem eventSystem => m_EventSystem;
#pragma warning restore IDE1006 // Naming Styles
        /// <summary>
        /// PrefabManager 인스펙터창에서 보여질 이름입니다.
        /// 런타임에 아무런 영향을 주지않습니다.
        /// </summary>
        public virtual string DisplayName => name;
        public virtual bool InitializeOnCall => true;

        /// <summary>
        /// 이 모노 프록시 객체가 <see cref="Presentation.GameObjectProxySystem"/>에서 사용 중인지 반환합니다.
        /// </summary>
        public bool Activated { get; private set; } = false;

        public virtual void Initialize()
        {
            if (Activated) throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject,
                "이미 초기화 된 재사용 오브젝트를 또 초기화하려합니다.");

            OnInitialize();
            //gameObject.SetActive(true);
            Activated = true;
        }

        #region Component Methods

        public T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// 이 오브젝트, 혹은 하위 오브젝트의 컴포넌트를 받아옵니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="GameObjectProxySystem"/>에서 파싱한 컴포넌트를 기반으로 합니다.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public new T GetComponent<T>() where T : Component
        {
            if (!m_Components.TryGetValue(ComponentID<T>.ID, out List<Component> list)) return null;

            return (T)list[0];
        }
        public new T[] GetComponents<T>() where T : Component
        {
            if (!m_Components.TryGetValue(ComponentID<T>.ID, out List<Component> list)) return null;

            return list.Select((other) => (T)other).ToArray();
        }
        /// <inheritdoc cref="GetComponent{T}"/>
        public new Component GetComponent(Type t)
        {
            IComponentID id = ComponentID.GetID(t);
            if (!m_Components.TryGetValue(id, out List<Component> list)) return null;

            return list[0];
        }
        /// <inheritdoc cref="GetComponent{T}"/>
        public new Component[] GetComponents(Type t)
        {
            IComponentID id = ComponentID.GetID(t);
            if (!m_Components.TryGetValue(id, out List<Component> list)) return null;

            return list.ToArray();
        }

        /// <summary>
        /// 이 오브젝트에 새로운 유니티 <see cref="Component"/>를 추가합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T AddComponent<T>() where T : Component
        {
            CoreSystem.Logger.ThreadBlock(nameof(AddComponent), ThreadInfo.Unity);

            T component = null;
            component = gameObject.AddComponent<T>();
            if (!m_Components.TryGetValue(ComponentID<T>.ID, out var list))
            {
                list = new List<Component>();
                m_Components.Add(ComponentID<T>.ID, list);
            }
            list.Add(component);

            return component;
        }

        #endregion

        /// <summary>
        /// 이 객체가 생성되었을때만 한번 실행하는 함수입니다.
        /// </summary>
        protected virtual void OnCreated() { }
        internal void InternalOnCreated()
        {
            m_GameObject = base.gameObject;
            m_Transform = base.transform;

            Component[] components = GetComponentsInChildren(TypeHelper.TypeOf<Component>.Type, true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    CoreSystem.Logger.Log(Channel.Proxy,
                        $"{name} has missing component. Fix it!");

                    continue;
                }

                Type t = components[i].GetType();
                IComponentID id = ComponentID.GetID(t);

                if (!m_Components.TryGetValue(id, out List<Component> list))
                {
                    list = new List<Component>();
                    m_Components.Add(id, list);
                }
                list.Add(components[i]);
            }

            OnCreated();
        }
        /// <summary>
        /// <see cref="Presentation.GameObjectProxySystem"/>에서 이 프록시 모노 객체를 재사용을 위해 실행되는 초기화 함수입니다.
        /// </summary>
        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }

        internal void Terminate()
        {
            CoreSystem.Logger.ThreadBlock(nameof(RecycleableMonobehaviour.Terminate), ThreadInfo.Unity);
            if (!Activated) throw new Exception("not initialized");

            OnTerminate();
            Activated = false;
        }

        public bool IsValid() => Activated && !m_Entity.Equals(Entity<IEntity>.Empty);

        void INotificationReceiver.OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is Timeline.AnimatorTriggerMarker animtrigger)
            {
                AnimatorComponent animator = GetComponent<AnimatorComponent>();
                if (animator == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"Timeline trying to triggering animator at entity({entity.Name}) but there\'s no animator");
                    return;
                }

                animator.m_Animator.SetTrigger(animtrigger.TriggerKey);
            }
        }
    }
}
