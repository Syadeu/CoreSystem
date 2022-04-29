// Copyright 2021 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
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
    public abstract class RecycleableMonobehaviour : MonoBehaviour, IProxyMonobehaviour, INotificationReceiver
    {
        /// <summary>
        /// <see cref="GameObjectProxySystem.m_Instances"/> value 리스트의 인덱스입니다.
        /// </summary>
        internal int m_Idx = -1;

        private GameObject m_GameObject;
        private Rigidbody m_Rigidbody;
        private Transform m_Transform;
        private Entity<IEntity> m_Entity;

        private readonly Dictionary<IComponentID, List<Component>> m_Components = new Dictionary<IComponentID, List<Component>>();

        private IProxyComponent[] m_ProxyComponents;
        private IPresentationReceiver[] m_PresentationReceivers;
        private IPresentationUpdater[] m_PresentationUpdaters = Array.Empty<IPresentationUpdater>();

        public event Action<Entity<IEntity>> OnVisible;
        public event Action<Entity<IEntity>> OnInvisible;

        public event Action<Entity<IEntity>> OnEntityRegistered;
        public event Action<Entity<IEntity>> OnEntityUnregistered;

        #region Properties

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// <see cref="GameObjectProxySystem"/> 에서 파싱한 <see cref="GameObject"/> 입니다.
        /// </summary>
        public new GameObject gameObject => m_GameObject;
        /// <summary>
        /// <see cref="GameObjectProxySystem"/> 에서 파싱한 <see cref="Transform"/> 입니다.
        /// </summary>
        public new Transform transform => m_Transform;
        public Entity<IEntity> entity
        {
            get => m_Entity;
            internal set
            {
                if (value.IsEmpty())
                {
                    OnEntityUnregistered?.Invoke(m_Entity);
                    m_Entity = value;
                }
                else
                {
                    m_Entity = value;
                    OnEntityRegistered?.Invoke(value);
                }
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        public virtual bool InitializeOnCall => true;
        /// <summary>
        /// 이 모노 프록시 객체가 <see cref="GameObjectProxySystem"/>에서 사용 중인지 반환합니다.
        /// </summary>
        public bool Activated { get; private set; } = false;

        #endregion

        #region Internals

        internal void InternalOnCreated()
        {
            m_GameObject = base.gameObject;
            m_Transform = base.transform;
            m_Rigidbody = GetComponent<Rigidbody>();

            Component[] components = GetComponentsInChildren(TypeHelper.TypeOf<Component>.Type, true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    CoreSystem.Logger.LogError(Channel.Proxy,
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

            IProxyComponent[] proxyComponents = base.GetComponentsInChildren<IProxyComponent>(true);
            for (int i = 0; i < proxyComponents.Length; i++)
            {
                proxyComponents[i].OnProxyCreated(this);
            }
            SetupPresentationReceiverCallbacks();
            SetupPresentationUpdater();

            OnCreated();
            PresentationReceiverOnCreated();
        }
        internal void InternalInitialize()
        {
            if (Activated) throw new CoreSystemException(CoreSystemExceptionFlag.RecycleObject,
                "이미 초기화 된 재사용 오브젝트를 또 초기화하려합니다.");

            OnInitialize();
            //gameObject.SetActive(true);

            if (m_Rigidbody != null)
            {
                CoreSystem.Instance.OnFixedUpdate += OnFixedUpdate;
            }

            Activated = true;
        }
        internal void InternalTerminate()
        {
            CoreSystem.Logger.ThreadBlock(nameof(RecycleableMonobehaviour.InternalTerminate), ThreadInfo.Unity);
            if (!Activated) throw new Exception("not initialized");

            CoreSystem.Instance.OnFixedUpdate -= OnFixedUpdate;

            OnTerminate();

            OnParticleStopped = null;
            Activated = false;
        }

        private void OnFixedUpdate()
        {
            if (!entity.IsValid()) return;

            //Transform thisTr = transform;
            ProxyTransform tr = entity.transform;
            tr.position = m_Rigidbody.position;
            tr.rotation = m_Rigidbody.rotation;
        }

        internal void InternalOnVisible()
        {
            OnVisible?.Invoke(m_Entity);
        }
        internal void InternalOnInvisible()
        {
            OnInvisible?.Invoke(m_Entity);
        }

        #endregion

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

        public T GetComponentUnity<T>() where T : Component
        {
            CoreSystem.Logger.ThreadBlock(ThreadInfo.Unity);

            return base.GetComponent<T>();
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
            if (TypeHelper.TypeOf<T>.IsAbstract)
            {
                foreach (var item in m_Components.Values)
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        if (TypeHelper.TypeOf<T>.Type.IsAssignableFrom(item[i].GetType()))
                        {
                            return (T)item[i];
                        }
                    }
                }
                return null;
            }

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
            if (t.IsAbstract)
            {
                foreach (var item in m_Components.Values)
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        if (t.IsAssignableFrom(item[i].GetType()))
                        {
                            return item[i];
                        }
                    }
                }

                return null;
            }

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

        #region Callbacks

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
                return;
            }

            CoreSystem.Logger.LogError(Channel.Entity,
                $"Unhandled marker type: {notification.GetType().Name}");
        }

        #region Particle System

        private event Action<Entity<IEntity>, RecycleableMonobehaviour> OnParticleStopped;

        public void AddOnParticleStoppedEvent(Action<Entity<IEntity>, RecycleableMonobehaviour> ev)
        {
            OnParticleStopped += ev;
        }
        public void RemoveOnParticleStoppedEvent(Action<Entity<IEntity>, RecycleableMonobehaviour> ev)
        {
            OnParticleStopped -= ev;
        }
        private void OnParticleSystemStopped()
        {
            if (!IsValid()) return;

            OnParticleStopped?.Invoke(m_Entity, this);
        }

        #endregion

        #region IPresentationReceiver

        private void SetupPresentationReceiverCallbacks()
        {
            m_PresentationReceivers = GetComponentsInChildren<IPresentationReceiver>(true);

            for (int i = 0; i < m_PresentationReceivers.Length; i++)
            {
                OnEntityRegistered += m_PresentationReceivers[i].OnIntialize;
                OnEntityUnregistered += m_PresentationReceivers[i].OnTerminate;
            }
        }
        private void PresentationReceiverOnCreated()
        {
            try
            {
                for (int i = 0; i < m_PresentationReceivers.Length; i++)
                {
                    m_PresentationReceivers[i].OnCreated();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        #endregion

        private void SetupPresentationUpdater()
        {
            m_PresentationUpdaters = GetComponentsInChildren<IPresentationUpdater>(true);

            for (int i = 0; i < m_PresentationUpdaters.Length; i++)
            {
                PresentationManager.Instance.Update += m_PresentationUpdaters[i].OnPresentation;
            }
        }

        internal void ProcessMessageContext(MessageContext ctx, object obj)
        {
            SendMessage(ctx.methodName, obj, ctx.options);
        }

        #endregion

        protected void OnDestroy()
        {
            for (int i = 0; i < m_PresentationUpdaters.Length; i++)
            {
                PresentationManager.Instance.Update -= m_PresentationUpdaters[i].OnPresentation;
            }
        }

        /// <summary>
        /// 이 객체가 생성되었을때만 한번 실행하는 함수입니다.
        /// </summary>
        protected virtual void OnCreated() { }
        /// <summary>
        /// <see cref="GameObjectProxySystem"/>에서 이 프록시 모노 객체를 재사용을 위해 실행되는 초기화 함수입니다.
        /// </summary>
        protected virtual void OnInitialize() { }
        protected virtual void OnTerminate() { }

        public bool IsValid() => Activated && !m_Entity.Equals(Entity<IEntity>.Empty);
    }

    [BurstCompatible]
    public struct MessageContext
    {
        private FixedString512Bytes m_MethodName;
        private int m_UserData;
        private SendMessageOptions m_Options;

        public string methodName
        {
            get => m_MethodName.ToString();
            set => m_MethodName = value;
        }
        public int UserData
        {
            get => m_UserData;
            set => m_UserData = value;
        }
        public SendMessageOptions options
        {
            get => m_Options;
            set => m_Options = value;
        }

        [NotBurstCompatible]
        public MessageContext(string methodName, SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            m_MethodName = methodName;
            m_UserData = 0;
            m_Options = options;
        }
        [NotBurstCompatible]
        public MessageContext(string methodName, int userData, SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            m_MethodName = methodName;
            m_UserData = userData;
            m_Options = options;
        }

        public override int GetHashCode()
        {
            return unchecked(m_MethodName.GetHashCode() ^ (int)m_Options);
        }
    }

    public interface IPresentationUpdater
    {
        void OnPresentation();
    }
}
