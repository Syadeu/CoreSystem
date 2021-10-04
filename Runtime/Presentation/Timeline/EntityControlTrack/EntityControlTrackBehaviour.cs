using Syadeu.Presentation.Attributes;
using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Syadeu.Presentation.Timeline
{
    [Serializable]
    public class EntityControlTrackBehaviour : PlayableBehaviour
    {
        //private AnimatorComponent m_Animator;

        public override void OnPlayableCreate(Playable playable)
        {
        }
        public void Initialize(/*AnimatorComponent animator*/)
        {
            //m_Animator = animator;
        }
    }
}
