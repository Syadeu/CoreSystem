﻿using Newtonsoft.Json;
using Syadeu.Internal;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Data;
using Syadeu.Presentation.Entities;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Data: Actor Overlay UI Entry")]
    public sealed class ActorOverlayUIEntry : DataObjectBase
    {
        [JsonProperty(Order = 0, PropertyName = "EnableAlways")]
        public bool m_EnableAlways = false;

        [Space]
        [JsonProperty(Order = 1, PropertyName = "Prefab")]
        public Reference<UIObjectEntity> m_Prefab = Reference<UIObjectEntity>.Empty;
        [JsonProperty(Order = 2, PropertyName = "UpdateType")]
        public UpdateType m_UpdateType = UpdateType.Instant | UpdateType.SyncCameraOrientation;
        [JsonProperty(Order = 3, PropertyName = "UpdateSpeed")]
        public float m_UpdateSpeed = 4;

        [Space, Header("Position")]
        [JsonProperty(Order = 4, PropertyName = "Offset")]
        public float3 m_Offset = float3.zero;

        [Space, Header("Orientation")]
        [JsonProperty(Order = 5, PropertyName = "OrientationOffset")]
        public float3 m_OrientationOffset = float3.zero;

#if CORESYSTEM_TURNBASESYSTEM

        [Space, Header("Turn-based System")]
        [ReflectionDescription("EnableAlways 이 True 이면 아래 설정들을 전부 무시합니다.")]

        [JsonProperty(Order = 6, PropertyName = "EnableWhileTurnTable")]
        [Tooltip("TurnTable 시스템이 동작 중일때만 활성화합니다. " +
            "true 이면 아래 predicate 를 전부 무시합니다.")]
        public bool m_EnableWhileTurnTable = false;

        [JsonProperty(Order = 7, PropertyName = "OnStartTurnPredicate")]
        [Tooltip("내 턴이 시작할 때, 지정한 모든 조건이 만족하면 활성화 합니다. " +
            "아무것도 없으면 True입니다")]
        public Reference<TriggerPredicateAction>[] m_OnStartTurnPredicate = Array.Empty<Reference<TriggerPredicateAction>>();
        [JsonProperty(Order = 8, PropertyName = "OnEndTurnPredicate")]
        [Tooltip("내 턴이 종료되었을 때, 지정한 모든 조건이 만족하면 비활성화 합니다. " +
            "아무것도 없으면 True입니다")]
        public Reference<TriggerPredicateAction>[] m_OnEndTurnPredicate = Array.Empty<Reference<TriggerPredicateAction>>();

        public enum TurnbaseOverlayType
        {
            Constant,

            DuringMyTurn,
        }
#endif
    }
}
