using Syadeu.Presentation.Proxy;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Syadeu.Presentation.Timeline
{
    [TrackColor(0.4812656f, 0.7359158f, 0.990566f)]
    [TrackClipType(typeof(EntityControlTrackClip))]
    [TrackBindingType(typeof(RecycleableMonobehaviour), TrackBindingFlags.None)]
    public class EntityControlTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<EntityControlTrackMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
