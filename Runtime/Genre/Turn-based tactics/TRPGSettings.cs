﻿// Copyright 2021 Seung Ha Kim
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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using UnityEngine;

namespace Syadeu.Presentation.TurnTable
{
    [PreferBinarySerialization]
    public sealed class TRPGSettings : StaticSettingEntity<TRPGSettings>
    {
        public override bool RuntimeModifiable => true;

        [Header("Grid")]
        [SerializeField]
        public Color32 m_MovableTileColor;
        [SerializeField]
        public Color32 m_MovableOutlineColor,
            m_PathlineColor, m_PathlineOverlayColor, m_PathlineEndTipColor,
            m_DetectionTileColorStart, m_DetectionTileColorEnd = Color.clear;
    }
}