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

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Syadeu.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

        [SerializeField]
        public Vector3 
            m_OutlineWallColorStartPos = new Vector3(0, -2.3f, 0), 
            m_OutlineWallColorEndPos;

        [Header("Coverable Wall")]
        [SerializeField]
        public PrefabReference<Sprite> m_CoverableSprite = PrefabReference<Sprite>.None;
        [SerializeField]
        public Vector2 m_CoverableSpriteSizeDeltaMultiplier = new Vector2(1, 1);
        [SerializeField]
        public Vector3
            m_CoverableWallColorStartPos = new Vector3(0, -2.3f, 0),
            m_CoverableWallColorEndPos;
    }
}