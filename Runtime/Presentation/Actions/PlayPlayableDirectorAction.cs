using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Play PlayableDirector")]
    [ReflectionDescription(
        "타임라인 액션입니다.\n" +
        ""
        )]
    public sealed class PlayPlayableDirectorAction : TriggerAction
    {
        [JsonProperty] private Reference<TimelineData> m_Data;
        [JsonProperty(Order = 1, PropertyName = "StartDelay")] private float m_StartDelay = 0;
        [JsonProperty(Order = 2, PropertyName = "EndDelay")] private float m_EndDelay = 0;

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 3, PropertyName = "OnStart")]
        private Reference<TriggerAction>[] m_OnStart = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 4, PropertyName = "OnEnd")]
        private Reference<TriggerAction>[] m_OnEnd = Array.Empty<Reference<TriggerAction>>();

        [Space, Header("Actions")]
        [JsonProperty(Order = 5, PropertyName = "OnStartAction")]
        private Reference<InstanceAction>[] m_OnStartAction = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 6, PropertyName = "OnEndActions")]
        private Reference<InstanceAction>[] m_OnEndAction = Array.Empty<Reference<InstanceAction>>();

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            CoreSystem.StartUnityUpdate(this, Executer());
        }

        private IEnumerator Executer()
        {
            TimelineData data = m_Data.GetObject();

            AsyncOperationHandle<GameObject> entityOper = Addressables.LoadAssetAsync<GameObject>(data.m_Entity.GetObject().Prefab.GetObjectSetting().m_RefPrefab);
            yield return new WaitUntil(() => entityOper.IsDone);
            
            Entity<IEntity> entity = PresentationSystem<EntitySystem>.System.CreateEntity(data.m_Entity, 0, entityOper.Result.transform.rotation, entityOper.Result.transform.localScale);
            ProxyTransform tr = (ProxyTransform)entity.transform;
            tr.enableCull = false;

            AsyncOperationHandle<PlayableAsset> oper = data.LoadTimelineAsset();
            yield return new WaitUntil(() => oper.IsDone);
            yield return new WaitForProxy(tr);

            PlayableAsset asset = oper.Result;

            RecycleableMonobehaviour proxy = tr.proxy;
            PlayableDirector director = proxy.GetOrAddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.playableAsset = asset;

            foreach (PlayableBinding item in asset.outputs)
            {
                Type type = item.outputTargetType;
                if (type.Equals(TypeHelper.TypeOf<GameObject>.Type))
                {
                    director.SetGenericBinding(item.sourceObject, proxy.gameObject);
                    continue;
                }

                var component = proxy.GetComponent(type);
                if (component == null)
                {
                    CoreSystem.Logger.LogError(Channel.Entity,
                        $"{m_Data.GetObject().Name} requires {TypeHelper.ToString(type)} " +
                        $"but {entity.Name} doesn\'t have.");
                    continue;
                }

                director.SetGenericBinding(item.sourceObject, component);
            }

            m_OnStart.Execute(entity.As<IEntity, IEntityData>());
            m_OnStartAction.Execute();

            float time = 0;
            while (time < m_StartDelay)
            {
                time += Time.deltaTime;
                yield return null;
            }

            director.Play();
            data.m_OnTimelineStart.Execute(entity.As<IEntity, IEntityData>());
            data.m_OnTimelineStartAction.Execute();

            yield return null;
            while (director.state == PlayState.Playing)
            {
                yield return null;
            }

            data.m_OnTimelineEnd.Execute(entity.As<IEntity, IEntityData>());
            data.m_OnTimelineEndAction.Execute();

            time = 0;
            while (time < m_EndDelay)
            {
                time += Time.deltaTime;
                yield return null;
            }

            m_OnEnd.Execute(entity.As<IEntity, IEntityData>());
            m_OnEndAction.Execute();

            foreach (PlayableBinding item in asset.outputs)
            {
                director.ClearGenericBinding(item.sourceObject);
            }
        }
    }
}
