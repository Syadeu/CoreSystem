using Cinemachine;
using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Mono;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;
using System;
using System.Collections;
using System.ComponentModel;
using Unity.Mathematics;
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

        [JsonIgnore] private EntityData<IEntityData> m_Executer;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            m_Executer = entity;
            CoreSystem.StartUnityUpdate(this, Executer());
        }

        private IEnumerator Executer()
        {
            TimelineData data = m_Data.GetObject();
            
            float3 pos;
            quaternion rot;
            if (data.m_WorldSpace)
            {
                pos = data.m_PositionOffset;
                rot = quaternion.EulerZXY(data.m_RotationOffset);
            }
            else
            {
                if (m_Executer.Target is EntityBase entityBase)
                {
                    pos = entityBase.transform.position + data.m_PositionOffset;
                    rot = math.mul(m_Executer.As<IEntityData, IEntity>().transform.rotation, quaternion.EulerZXY(data.m_RotationOffset));
                }
                else
                {
                    pos = data.m_PositionOffset;
                    rot = quaternion.EulerZXY(data.m_RotationOffset);
                }
            }

            EntityBase original = data.m_Entity.GetObject();
            Entity<IEntity> entity;

            if (original.Prefab.Asset == null)
            {
                AsyncOperationHandle<GameObject> entityOper = original.Prefab.LoadAssetAsync();
                yield return new WaitUntil(() => entityOper.IsDone);
                entity = PresentationSystem<EntitySystem>.System.CreateEntity(data.m_Entity, pos, rot, entityOper.Result.transform.localScale);
            }
            else
            {
                entity = PresentationSystem<EntitySystem>.System.CreateEntity(data.m_Entity, pos, rot, original.Prefab.Asset.transform.localScale);
            }
            
            ProxyTransform tr = (ProxyTransform)entity.transform;
            tr.enableCull = false;
            
            yield return new WaitForProxy(tr);

            RecycleableMonobehaviour proxy = tr.proxy;
            PlayableDirector director = proxy.GetOrAddComponent<PlayableDirector>();
            director.playOnAwake = false;

            PlayableAsset asset;
            if (!data.m_UseObjectTimeline)
            {
                if (data.m_Timeline.Asset == null)
                {
                    AsyncOperationHandle<PlayableAsset> oper = data.m_Timeline.LoadAssetAsync();
                    yield return new WaitUntil(() => oper.IsDone);
                    asset = oper.Result;
                }
                else
                {
                    asset = data.m_Timeline.Asset;
                }

                director.playableAsset = asset;
                foreach (PlayableBinding item in asset.outputs)
                {
                    Type type = item.outputTargetType;
                    if (type.Equals(TypeHelper.TypeOf<GameObject>.Type))
                    {
                        director.SetGenericBinding(item.sourceObject, proxy.gameObject);
                        continue;
                    }
                    if (type.Equals(TypeHelper.TypeOf<CinemachineBrain>.Type))
                    {
                        director.SetGenericBinding(item.sourceObject, PresentationSystem<RenderSystem>.System.Camera.GetComponent<CinemachineBrain>());
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
            }
            else
            {
                asset = director.playableAsset;

                foreach (PlayableBinding item in asset.outputs)
                {
                    Type type = item.outputTargetType;
                    if (type.Equals(TypeHelper.TypeOf<CinemachineBrain>.Type))
                    {
                        director.SetGenericBinding(item.sourceObject, PresentationSystem<RenderSystem>.System.Camera.GetComponent<CinemachineBrain>());
                        continue;
                    }
                }
            }

            m_OnStart.Execute(m_Executer);
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

            m_OnEnd.Execute(m_Executer);
            m_OnEndAction.Execute();

            if (!data.m_UseObjectTimeline)
            {
                foreach (PlayableBinding item in asset.outputs)
                {
                    director.ClearGenericBinding(item.sourceObject);
                }
            }

            m_Executer = EntityData<IEntityData>.Empty;
        }
    }
}
