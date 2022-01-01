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

using Newtonsoft.Json;

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
using Syadeu.Presentation.Timeline;
using Syadeu.Presentation.Events;
using Syadeu.Collections.Proxy;
using Syadeu.Collections;

using Cinemachine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("TriggerAction: Play PlayableDirector")]
    [Description(
        "타임라인 액션입니다.\n" +
        "OnStart -> OnStartAction -> OnTimelineStart -> OnTimelineEnd -> OnEnd -> OnEndAction"
        )]
    public sealed class PlayPlayableDirectorAction : TriggerAction, IEventSequence
    {
        [JsonProperty(Order = 0)] private Reference<TimelineData> m_Data;
        [JsonProperty(Order = 1, PropertyName = "UpdateMode")]
        private DirectorUpdateMode m_UpdateMode = DirectorUpdateMode.GameTime;
        [JsonProperty(Order = 2, PropertyName = "StartDelay")] private float m_StartDelay = 0;
        [JsonProperty(Order = 3, PropertyName = "EndDelay")] private float m_EndDelay = 0;

        [Space, Header("PredicateActions: Conditional")]
        [Tooltip("False를 반환하면 이 Timeline 을 실행하지 않습니다.")]
        [JsonProperty(Order = 4, PropertyName = "Conditional")]
        private Reference<TriggerPredicateAction>[] m_Conditional = Array.Empty<Reference<TriggerPredicateAction>>();

        [Space, Header("TriggerActions")]
        [JsonProperty(Order = 5, PropertyName = "OnStart")]
        private Reference<TriggerAction>[] m_OnStart = Array.Empty<Reference<TriggerAction>>();
        [JsonProperty(Order = 6, PropertyName = "OnEnd")]
        private Reference<TriggerAction>[] m_OnEnd = Array.Empty<Reference<TriggerAction>>();

        [Space, Header("Actions")]
        [JsonProperty(Order = 7, PropertyName = "OnStartAction")]
        private Reference<InstanceAction>[] m_OnStartAction = Array.Empty<Reference<InstanceAction>>();
        [JsonProperty(Order = 8, PropertyName = "OnEndActions")]
        private Reference<InstanceAction>[] m_OnEndAction = Array.Empty<Reference<InstanceAction>>();

        [Space, Header("Sequence")]
        [JsonProperty(Order = 9, PropertyName = "AfterDelay")]
        private float m_AfterDelay = 0;
        [JsonProperty(Order = 10, PropertyName = "DestroyTimelineAfterFinished")]
        private bool m_DestroyTimelineAfterFinished = true;

        [JsonIgnore] private CoroutineSystem m_CoroutineSystem = null;
        [JsonIgnore] private bool m_KeepWait = false;

        [JsonIgnore] public bool KeepWait => m_KeepWait;
        [JsonIgnore] public float AfterDelay => m_AfterDelay;

        protected override void OnCreated()
        {
            m_CoroutineSystem = PresentationSystem<DefaultPresentationGroup, CoroutineSystem>.System;
        }
        protected override void OnDestroy()
        {
            m_CoroutineSystem = null;
        }
        protected override void OnExecute(Entity<IObject> entity)
        {
            m_KeepWait = true;

            PayloadJob job = new PayloadJob
            {
                m_Caller = Idx.GetEntity<PlayPlayableDirectorAction>(),
                m_Data = m_Data,
                m_Executer = entity,

                m_UpdateMode = m_UpdateMode,
                m_StartDelay = m_StartDelay,
                m_EndDelay = m_EndDelay,

                m_Conditional = m_Conditional.ToFixedList64(),

                m_OnStart = m_OnStart.ToFixedList64(),
                m_OnStartAction = m_OnStartAction.ToFixedList64(),

                m_OnEnd = m_OnEnd.ToFixedList64(),
                m_OnEndAction = m_OnEndAction.ToFixedList64()
            };

            m_CoroutineSystem.StartCoroutine(job);
        }
        protected override void OnReserve()
        {
            m_KeepWait = false;
        }

        private struct PayloadJob : ICoroutineJob
        {
            public Entity<PlayPlayableDirectorAction> m_Caller;
            public Reference<TimelineData> m_Data;
            public Entity<IObject> m_Executer;

            public DirectorUpdateMode m_UpdateMode;
            public float m_StartDelay;
            public float m_EndDelay;

            public FixedReferenceList64<TriggerPredicateAction> m_Conditional;

            public FixedReferenceList64<TriggerAction> m_OnStart;
            public FixedReferenceList64<TriggerAction> m_OnEnd;

            public FixedReferenceList64<InstanceAction> m_OnStartAction;
            public FixedReferenceList64<InstanceAction> m_OnEndAction;

            UpdateLoop ICoroutineJob.Loop => UpdateLoop.Default;

            public void Dispose()
            {
                m_Caller.Target.m_KeepWait = false;

                m_Caller = Entity<PlayPlayableDirectorAction>.Empty;
                m_Data = Reference<TimelineData>.Empty;
                m_Executer = Entity<IObject>.Empty;
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
                        pos = m_Executer.transform.position + data.m_PositionOffset;
                        rot = math.mul(m_Executer.ToEntity<IEntity>().transform.rotation, quaternion.EulerZXY(data.m_RotationOffset));
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
                    entity = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.CreateEntity(data.m_Entity, pos, rot, entityOper.Result.transform.localScale);
                }
                else
                {
                    entity = PresentationSystem<DefaultPresentationGroup, EntitySystem>.System.CreateEntity(data.m_Entity, pos, rot, original.Prefab.Asset.transform.localScale);
                }

                ProxyTransform tr = (ProxyTransform)entity.transform;
                tr.enableCull = false;

                yield return new WaitForProxy(tr);

                IProxyMonobehaviour proxy = tr.proxy;
                PlayableDirector director = proxy.GetOrAddComponent<PlayableDirector>();
                director.playOnAwake = false;
                director.timeUpdateMode = m_UpdateMode;

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
                            director.SetGenericBinding(item.sourceObject, PresentationSystem<DefaultPresentationGroup, RenderSystem>.System.Camera.GetComponent<CinemachineBrain>());
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
                data.m_OnTimelineStart.Execute(entity.ToEntity<IObject>());
                data.m_OnTimelineStartAction.Execute();

                yield return null;
                while (director.state == PlayState.Playing)
                {
                    yield return null;
                }

                data.m_OnTimelineEnd.Execute(entity.ToEntity<IObject>());
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

                if (m_Caller.Target.m_DestroyTimelineAfterFinished)
                {
                    entity.Destroy();
                }
            }

            private void Bind(PlayableDirector director, PlayableAsset asset)
            {
                //AnimatorAttribute animator = m_Executer.GetAttribute<AnimatorAttribute>();
                CinemachineBrain cinemachine = PresentationSystem<DefaultPresentationGroup, RenderSystem>.System.Camera.GetComponent<CinemachineBrain>();

                //CoreSystem.Logger.NotNull(animator, "animator not found");
                CoreSystem.Logger.NotNull(cinemachine, "cinemachine not found");

                foreach (PlayableBinding item in asset.outputs)
                {
#if UNITY_CINEMACHINE
                    if (item.sourceObject is CinemachineTrack)
                    {
                        director.SetGenericBinding(item.sourceObject, cinemachine);
                        continue;
                    }
#endif

                    if (item.sourceObject is EntityControlTrack entityControlTrack)
                    {
                        director.SetGenericBinding(item.sourceObject, m_Executer.ToEntity<IEntity>().proxy);

                        //foreach (EntityControlTrackClip clip in
                        //    entityControlTrack.GetClips().Select((other) => (EntityControlTrackClip)other.asset))
                        //{
                        //    director.SetReferenceValue(clip.Animator.exposedName, animator.AnimatorComponent);
                        //}
                    }
                }
            }
            //

        }
    }
}
