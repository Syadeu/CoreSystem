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


using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [DisplayName("System/Is Current Game State")]
    [Guid("6FD8191B-DE49-4BCE-89D8-2C19647875A3")]
    internal sealed class IsCurrentGameStateConstAction : ConstAction<bool>
    {
        public enum State : ushort
        {
            Master,
            Debug,
            Start,
            Ingame
        }

        [SerializeField, JsonProperty(Order = 0, PropertyName = "State")]
        private State m_State = State.Master;

        protected override bool Execute()
        {
            var sceneSystem = PresentationSystem<DefaultPresentationGroup, SceneSystem>.System;
            switch (m_State)
            {
                default:
                case State.Master:
                    return sceneSystem.IsMasterScene;
                case State.Debug:
                    return sceneSystem.IsDebugScene;
                case State.Start:
                    return sceneSystem.IsStartScene;
                case State.Ingame:
                    return sceneSystem.IsIngame;
            }
        }
    }
}
