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

using UnityEngine;
using Syadeu.Presentation.Data;
using Newtonsoft.Json;
using System.ComponentModel;
using Syadeu.Presentation.Actions;

namespace Syadeu.Presentation.Input
{
    [DisplayName("ConstantData: Input User Action")]
    public sealed class UserActionConstantData : ConstantData
    {
        [SerializeField, JsonProperty(Order = 0, PropertyName = "UserActionType")]
        internal UserActionType m_UserActionType = 0;
        [SerializeField, JsonProperty(Order = 1, PropertyName = "ConstActions")]
        internal ConstActionReferenceArray m_ConstActions = ConstActionReferenceArray.Empty;

        internal void Execute(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            m_ConstActions.Execute();
        }
    }
    internal sealed class UserActionConstantDataProcessor : EntityProcessor<UserActionConstantData>
    {
        private InputSystem m_InputSystem;

        protected override void OnInitialize()
        {
            RequestSystem<DefaultPresentationGroup, InputSystem>(Bind);
        }
        protected override void OnDispose()
        {
            m_InputSystem = null;
        }
        private void Bind(InputSystem other)
        {
            m_InputSystem = other;
        }

        protected override void OnCreated(UserActionConstantData obj)
        {
            var inputAction = m_InputSystem.GetUserActionKeyBinding(obj.m_UserActionType);
            inputAction.performed += obj.Execute;
        }
        protected override void OnDestroy(UserActionConstantData obj)
        {
            var inputAction = m_InputSystem.GetUserActionKeyBinding(obj.m_UserActionType);
            inputAction.performed -= obj.Execute;
        }
    }
}
