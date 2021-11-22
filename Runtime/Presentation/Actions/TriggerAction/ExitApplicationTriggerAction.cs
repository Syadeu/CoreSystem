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

using Syadeu.Collections;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Events;
using System;
using UnityEngine;

namespace Syadeu.Presentation.Actions
{
    [Obsolete]
    public sealed class ExitApplicationTriggerAction : TriggerAction, IEventSequence
    {
        public bool KeepWait => false;
        public float AfterDelay => 0;

        protected override void OnExecute(EntityData<IEntityData> entity)
        {
            Application.Quit();
        }
    }
}