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

using Syadeu.Presentation.Attributes;
using Syadeu.Presentation.Components;
using Syadeu.Presentation.Entities;
using Syadeu.Presentation.Render;
using System.ComponentModel;

namespace Syadeu.Presentation.TurnTable.UI
{
    [EntityAcceptOnly(null)]
    [DisplayName("EntityData: TRPG Mouse Cursor")]
    public sealed class TRPGMouseCursorEntity : EntityDataBase
#if CORESYSTEM_SHAPES
        , INotifyComponent<ShapesComponent>
#endif
    {
#if CORESYSTEM_SHAPES

#endif
    }
    internal sealed class TRPGMouseCursorEntityProcessor : EntityProcessor<TRPGMouseCursorEntity>
    {

    }
}