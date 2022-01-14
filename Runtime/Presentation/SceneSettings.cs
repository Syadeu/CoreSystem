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

#if UNITY_EDITOR
#endif

using Syadeu.Collections;
using Syadeu.Presentation;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Mono
{
    [PreferBinarySerialization]
    public sealed class SceneSettings : StaticSettingEntity<SceneSettings>
    {
        public SceneReference CustomLoadingScene;

        [Space]
        public SceneReference MasterScene;
        public SceneReference StartScene;

        public List<SceneReference> Scenes = new List<SceneReference>();

        [SerializeField]
        private PrefabReference<GameObject> m_CameraPrefab = PrefabReference<GameObject>.None;

        public PrefabReference<GameObject> CameraPrefab => m_CameraPrefab;

        public SceneReference GetScene(string path)
        {
            if (CustomLoadingScene != null && CustomLoadingScene.ScenePath.Equals(path)) return CustomLoadingScene;
            else if (MasterScene != null && MasterScene.ScenePath.Equals(path)) return MasterScene;
            else if (StartScene != null && StartScene.ScenePath.Equals(path)) return StartScene;

            for (int i = 0; i < Scenes.Count; i++)
            {
                if (Scenes[i].ScenePath.Equals(path)) return Scenes[i];
            }
            return null;
        }
    }
}
 