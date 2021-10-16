using System.ComponentModel;
using Newtonsoft.Json;
using Syadeu.Presentation.Data;
using MxM;
using UnityEngine;

#if CORESYSTEM_MOTIONMATCHING
namespace Syadeu.Presentation.MotionMatching
{
    [DisplayName("Data: MxM Event Definition")]
    public sealed class MxMEventDefinitionData : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "EventID")] 
        private int m_EventID = 0;
        [JsonProperty(Order = 1, PropertyName = "EventType")]
        private EMxMEventType m_EventType = EMxMEventType.Standard;
        [JsonProperty(Order = 2, PropertyName = "Prioity")]
        private int m_Prioity = -1;
        [JsonProperty(Order = 3, PropertyName = "ContactCountToMatch")]
        private int ContactCountToMatch = 1;
        [JsonProperty(Order = 4, PropertyName = "ContactCountToWarp")]
        private int ContactCountToWarp = 1;
        [JsonProperty(Order = 5, PropertyName = "ExitWithMotion")]
        private bool m_ExitWithMotion = false;
        [JsonProperty(Order = 6, PropertyName = "MatchPose")]
        private bool m_MatchPose = true;
        [JsonProperty(Order = 7, PropertyName = "MatchTrajectory")]
        private bool m_MatchTrajectory = true;
        [JsonProperty(Order = 8, PropertyName = "MatchRequireTags")]
        private bool m_MatchRequireTags = false;
        [JsonProperty(Order = 9, PropertyName = "PostEventTrajectoryMode")]
        private EPostEventTrajectoryMode m_PostEventTrajectoryMode = EPostEventTrajectoryMode.Maintain;

        [Space]
        [JsonProperty(Order = 10, PropertyName = "MatchTiming")]
        private bool m_MatchTiming;
        [JsonProperty(Order = 11, PropertyName = "TimingWeight")]
        private float TimingWeight;
        [JsonProperty(Order = 12, PropertyName = "TimingWarpType")]
        private EEventWarpType TimingWarpType;

        [JsonProperty(Order = 13, PropertyName = "MatchPosition")]
        private bool MatchPosition;
        [JsonProperty(Order = 14, PropertyName = "PositionWeight")]
        private float PositionWeight;
        [JsonProperty(Order = 15, PropertyName = "MotionWarpType")]
        private EEventWarpType MotionWarpType;
        [JsonProperty(Order = 16, PropertyName = "WarpTimeScaling")]
        private bool WarpTimeScaling;
        [JsonProperty(Order = 17, PropertyName = "ContactCountToTimeScale")]
        private int ContactCountToTimeScale = 1;
        [JsonProperty(Order = 18, PropertyName = "MinWarpTimeScale")]
        private float MinWarpTimeScale = 0.9f;
        [JsonProperty(Order = 19, PropertyName = "MaxWarpTimeScale")]
        private float MaxWarpTimeScale = 1.2f;

        [Space]
        [JsonProperty(Order = 20, PropertyName = "MatchRotation")]
        private bool MatchRotation;
        [JsonProperty(Order = 21, PropertyName = "RotationWeight")]
        private float RotationWeight;
        [JsonProperty(Order = 22, PropertyName = "RotationWarpType")]
        private EEventWarpType RotationWarpType;

        [JsonIgnore] private MxMEventDefinition m_Definition;

        [JsonIgnore] public MxMEventDefinition EventDefinition => m_Definition;

        public void Initialize()
        {
            m_Definition = ScriptableObject.CreateInstance<MxMEventDefinition>();
            m_Definition.Id = m_EventID;
            m_Definition.EventType = m_EventType;
            m_Definition.Priority = m_Prioity;
            m_Definition.ContactCountToMatch = ContactCountToMatch;
            m_Definition.ContactCountToWarp = ContactCountToWarp;
            m_Definition.ExitWithMotion = m_ExitWithMotion;
            m_Definition.MatchPose = m_MatchPose;
            m_Definition.MatchTrajectory = m_MatchTrajectory;
            m_Definition.MatchRequireTags = m_MatchRequireTags;
            m_Definition.PostEventTrajectoryMode = m_PostEventTrajectoryMode;

            m_Definition.MatchTiming = m_MatchTiming;
            m_Definition.TimingWeight = TimingWeight;
            m_Definition.TimingWarpType = TimingWarpType;

            m_Definition.MatchPosition = MatchPosition;
            m_Definition.PositionWeight = PositionWeight;
            m_Definition.MotionWarpType = MotionWarpType;
            m_Definition.WarpTimeScaling = WarpTimeScaling;
            m_Definition.ContactCountToTimeScale = ContactCountToTimeScale;
            m_Definition.MinWarpTimeScale = MinWarpTimeScale;
            m_Definition.MaxWarpTimeScale = MaxWarpTimeScale;

            m_Definition.MatchRotation = MatchRotation;
            m_Definition.RotationWeight = RotationWeight;
            m_Definition.RotationWarpType = RotationWarpType;
        }
    }
}
#endif
