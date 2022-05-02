// Copyright 2022 Seung Ha Kim
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
using System.ComponentModel;
using Syadeu.Presentation.Actions;
using System.Runtime.InteropServices;
using Syadeu.Presentation.Components;

namespace Syadeu.Presentation.Actor
{
    [DisplayName("Actor/Insert item to inventory")]
    [Description(
        "")]
    [Guid("69096259-D9DB-48C2-8863-C0C1388FCB75")]
    internal sealed class InsertToInventoryCurrentItemConstAction : ConstTriggerAction<int>
    {
        [UnityEngine.SerializeField]
        private bool m_DestroyIfDosenotHaveInventory = true;

        protected override int Execute(InstanceID entity)
        {
            InstanceID item = ActorInteractionModule.InteractingEntityAtThisFrame.Idx;
            if (item.IsEmpty())
            {
                "?".ToLogError();
                return 0;
            }
            else if (!entity.HasComponent<ActorInventoryComponent>())
            {
                if (m_DestroyIfDosenotHaveInventory)
                {
                    item.Destroy();
                    return 0;
                }
                else
                {
                    $"doesnot have any inventory at {entity.GetEntity().Name}".ToLog();
                    return 0;
                }
            }

            ref ActorInventoryComponent inventoryCom = ref entity.GetComponent<ActorInventoryComponent>();
            inventoryCom.Inventory.Add(item);

            "insert item".ToLog();
            return 0;
        }
    }
}
