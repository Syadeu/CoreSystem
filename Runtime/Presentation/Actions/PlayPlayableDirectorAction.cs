using Newtonsoft.Json;

using Syadeu.Internal;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Proxy;
using Syadeu.Presentation.Render;

using System;
using System.Collections;
using System.ComponentModel;

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;

using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Timeline;
using System.Linq;
using Syadeu.Presentation.Attributes;

#if UNITY_CINEMACHINE
using Cinemachine;
#endif

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

        [Space, Header("PredicateActions: Conditional")]
        [JsonProperty(Order = 3, PropertyName = "Conditional")]
        private Reference<TriggerPredicateAction>[] m_Conditional = Array.Empty<Reference<TriggerPredicateAction>>();

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 4, PropertyName = "OnStart")]
        private Reference<TriggerAction>[] m_OnStart = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 5, PropertyName = "OnEnd")]
        private Reference<TriggerAction>[] m_OnEnd = Array.Empty<Reference<TriggerAction>>();

        [Space, Header("Actions")]
        [JsonProperty(Order = 6, PropertyName = "OnStartAction")]
        private Reference<InstanceAction>[] m_OnStartAction = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 7, PropertyName = "OnEndActions")]
        private Reference<InstanceAction>[] m_OnEndAction = Array.Empty<Reference<InstanceAction>>();

        [JsonIgnore] private CoroutineSystem m_CoroutineSystem = null;

        protected override void OnCreated()
        {
            m_CoroutineSystem = PresentationSystem<CoroutineSystem>.System;
        }
        protected override void OnDispose()
        {
            m_CoroutineSystem = null;
        }
        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            PayloadJob job = new PayloadJob
            {
                m_Data = m_Data,
                m_Executer = entity,

                m_StartDelay = m_StartDelay,
                m_EndDelay = m_EndDelay,

                m_Conditional = m_Conditional.ToBuffer(Allocator.Persistent),

                m_OnStart = m_OnStart.ToBuffer(Allocator.Persistent),
                m_OnStartAction = m_OnStartAction.ToBuffer(Allocator.Persistent),

                m_OnEnd = m_OnEnd.ToBuffer(Allocator.Persistent),
                m_OnEndAction = m_OnEndAction.ToBuffer(Allocator.Persistent)
            };

            m_CoroutineSystem.PostSequenceIterationJob(job);
        }

        private struct NotificationReceiver : INotificationReceiver
        {
            public void OnNotify(Playable origin, INotification notification, object context)
            {
                throw new NotImplementedException();
            }
        }
        private struct PayloadJob : ICoroutineJob, IDisposable
        {
            public Reference<TimelineData> m_Data;
            public EntityData<IEntityData> m_Executer;

            public float m_StartDelay;
            public float m_EndDelay;

            public ReferenceArray<Reference<TriggerPredicateAction>> m_Conditional;

            public ReferenceArray<Reference<TriggerAction>> m_OnStart;
            public ReferenceArray<Reference<TriggerAction>> m_OnEnd;

            public ReferenceArray<Reference<InstanceAction>> m_OnStartAction;
            public ReferenceArray<Reference<InstanceAction>> m_OnEndAction;

            public void Dispose()
            {
                m_Data = Reference<TimelineData>.Empty;
                m_Executer = EntityData<IEntityData>.Empty;

                m_Conditional.Dispose();
                m_OnStart.Dispose();
                m_OnEnd.Dispose();
                m_OnStartAction.Dispose();
                m_OnEndAction.Dispose();
            }
            public IEnumerator Execute()
            {
                if (!m_Conditional.Execute(m_Executer, out bool predicate) || !predicate)
                {
                    yield break;
                }

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
#if UNITY_CINEMACHINE
                        if (type.Equals(TypeHelper.TypeOf<CinemachineBrain>.Type))
                        {
                            director.SetGenericBinding(item.sourceObject, PresentationSystem<RenderSystem>.System.Camera.GetComponent<CinemachineBrain>());
                            continue;
                        }
#endif

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

                    Bind(director, asset);
                }

                m_OnStart.Execute(m_Executer);
                m_OnStartAction.Execute();

                float time = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup < time + m_StartDelay)
                {
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

                time = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup < time + m_EndDelay)
                {
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

                Dispose();
            }

            private void Bind(PlayableDirector director, PlayableAsset asset)
            {
                foreach (PlayableBinding item in asset.outputs)
                {
#if UNITY_CINEMACHINE
                    if (item.sourceObject is CinemachineTrack)
                    {
                        director.SetGenericBinding(item.sourceObject, PresentationSystem<RenderSystem>.System.Camera.GetComponent<CinemachineBrain>());
                        continue;
                    }
#endif
                    if (item.sourceObject is EntityControlTrack entityControlTrack)
                    {
                        AnimatorAttribute animator = m_Executer.GetAttribute<AnimatorAttribute>();
                        CoreSystem.Logger.NotNull(animator, "animator not found");

                        director.SetGenericBinding(item.sourceObject, m_Executer.As<IEntityData, IEntity>().proxy);

                        foreach (EntityControlTrackClip clip in
                            entityControlTrack.GetClips().Select((other) => (EntityControlTrackClip)other.asset))
                        {
                            director.SetReferenceValue(clip.m_Animator.exposedName, animator.AnimatorComponent);
                        }
                    }
                }
            }
            //
        }
    }
}
