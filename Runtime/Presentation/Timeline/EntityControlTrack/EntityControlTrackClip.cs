using Syadeu.Presentation.Attributes;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class EntityControlTrackClip : PlayableAsset, ITimelineClipAsset
{
    public EntityControlTrackBehaviour template = new EntityControlTrackBehaviour ();

    public ExposedReference<AnimatorComponent> m_Animator;

    public ClipCaps clipCaps
    {
        get { return ClipCaps.None; }
    }

    public override Playable CreatePlayable (PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<EntityControlTrackBehaviour>.Create (graph, template);
        EntityControlTrackBehaviour clone = playable.GetBehaviour ();
        return playable;
    }
}
