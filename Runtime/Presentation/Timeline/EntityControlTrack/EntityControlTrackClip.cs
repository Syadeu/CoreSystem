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