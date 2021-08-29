﻿using Newtonsoft.Json;
using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("Action: Play Animation Clip")]
    public sealed class PlayAnimationClipEventAction : TriggerAction
    {
        [JsonProperty(Order = 0, PropertyName = "Data")]
        private Reference<EntityAnimationClipEventData> m_Data;
        [JsonProperty(Order = 1, PropertyName = "StartDelay")] private float m_StartDelay = 0;
        [JsonProperty(Order = 2, PropertyName = "EndDelay")] private float m_EndDelay = 0;

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 3, PropertyName = "OnStart")]
        private Reference<TriggerAction>[] m_OnStart = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 4, PropertyName = "OnEnd")]
        private Reference<TriggerAction>[] m_OnEnd = Array.Empty<Reference<TriggerAction>>();

        [Space, Header("Actions")]
        [JsonProperty(Order = 5, PropertyName = "OnStartActions")]
        private Reference<InstanceAction>[] m_OnStartActions = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 6, PropertyName = "OnEndActions")]
        private Reference<InstanceAction>[] m_OnEndActions = Array.Empty<Reference<InstanceAction>>();

        [JsonIgnore] private EntityData<IEntityData> Executer { get; set; } = EntityData<IEntityData>.Empty;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            Executer = entity;

            var oper
                = Addressables.LoadAssetAsync<AnimationClip>(m_Data.GetObject().m_AnimationClip.GetObjectSetting().m_RefPrefab);
            oper.Completed += Oper_Completed;
        }
        private void Oper_Completed(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<AnimationClip> obj)
        {
            CoreSystem.StartUnityUpdate(this, Update(Executer, m_Data.GetObject(), obj.Result));
        }

        private IEnumerator Update(EntityData<IEntityData> executer, EntityAnimationClipEventData data, AnimationClip clip)
        {
            float passed = 0;

            var oper = Addressables.LoadAssetAsync<GameObject>(data.m_Entity.GetObject().Prefab.GetObjectSetting().m_RefPrefab);
            yield return new WaitUntil(() => oper.IsDone);

            Entity<IEntity> entity = PresentationSystem<EntitySystem>.System.CreateEntity(data.m_Entity, 0, oper.Result.transform.rotation, oper.Result .transform.localScale);
            ProxyTransform tr = (ProxyTransform)entity.transform;
            tr.enableCull = false;
            
            yield return new WaitUntil(() => tr.proxy != null);

            Mono.RecycleableMonobehaviour proxy = tr.proxy;
            AnimatorComponent component = proxy.GetComponent<AnimatorComponent>();
            if (component == null)
            {
                CoreSystem.Logger.LogError(Channel.Entity,
                    $"This entity({entity.Name}) does not have animator but requested play animation.");
                entity.Destroy();
                yield break;
            }

            proxy.gameObject.SetActive(false);
            "0".ToLog();

            Animation animation = proxy.GetComponent<Animation>();
            if (animation == null)
            {
                animation = proxy.AddComponent<Animation>();
                animation.playAutomatically = false;
            }

            m_OnStart.Execute(executer);
            m_OnStartActions.Execute();

            passed = 0;
            if (m_StartDelay > 0)
            {
                while (passed < m_StartDelay)
                {
                    passed += Time.deltaTime;
                    yield return null;
                }
            }

            proxy.gameObject.SetActive(true);

            data.m_OnClipStart.Execute(entity.As<IEntity, IEntityData>());
            data.m_OnClipStartAction.Execute();

            animation.clip = clip;
            animation.Play();

            "1".ToLog();

            passed = 0;
            while (passed < clip.length)
            {
                passed += Time.deltaTime;
                yield return null;
            }

            "2".ToLog();

            data.m_OnClipEnd.Execute(entity.As<IEntity, IEntityData>());
            data.m_OnClipEndAction.Execute();

            passed = 0;
            if (m_EndDelay > 0)
            {
                while (passed < m_EndDelay)
                {
                    passed += Time.deltaTime;
                    yield return null;
                }
            }

            m_OnEnd.Execute(executer);
            m_OnEndActions.Execute();

            Executer = EntityData<IEntityData>.Empty;
        }
    }
}
