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

using Syadeu.Presentation.Attributes;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Syadeu.Presentation.Timeline
{
    [Serializable]
    public class EntityControlTrackClip : PlayableAsset, ITimelineClipAsset
    {
        //public EntityControlTrackBehaviour template = new EntityControlTrackBehaviour ();

        //[SerializeField] private ExposedReference<AnimatorComponent> m_Animator;

        public ClipCaps clipCaps
        {
            get { return ClipCaps.None; }
        }

        //public ExposedReference<AnimatorComponent> Animator => m_Animator;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            //var playable = ScriptPlayable<EntityControlTrackBehaviour>.Create (graph, template);
            var playable = ScriptPlayable<EntityControlTrackBehaviour>.Create(graph);

            //ScriptPlayable<TimeNotificationBehaviour> timeNotificationBehaviour = ScriptPlayable<TimeNotificationBehaviour>.Create(graph);

            IExposedPropertyTable resolver = graph.GetResolver();

            EntityControlTrackBehaviour clone = playable.GetBehaviour();
            clone.Initialize(/*m_Animator.Resolve(resolver)*/);
            return playable;
        }
    }
}