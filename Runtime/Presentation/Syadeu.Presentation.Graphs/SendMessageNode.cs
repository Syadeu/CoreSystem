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

using GraphProcessor;
using Syadeu.Collections;
using Syadeu.Presentation.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syadeu.Presentation.Graphs
{
    [Serializable, NodeMenuItem("Presentation/Proxy/Send Message")]
    public sealed class SendMessageNode : PresentationNode
    {
        [Input("Transform")]
        private ProxyTransform transform;
        [SerializeField, Input("Method Name")]
        private string methodName;
        [SerializeField, Setting("Options")]
        private SendMessageOptions options = SendMessageOptions.RequireReceiver;
        [Input("Argument")]
        private object argument;

        public override string name => "Send Message";

        protected override void Process()
        {
            transform.SendMessage(methodName, argument, options);
        }

        private static bool IsExceptableType(Type t)
        {
            if (TypeHelper.TypeOf<GameObject>.Type.Equals(t) ||
                TypeHelper.InheritsFrom<Component>(t) ||
                TypeHelper.InheritsFrom<IEntityDataID>(t) ||
                TypeHelper.InheritsFrom<IInstanceID>(t)
                )
            {
                return true;
            }
            return false;
        }

        [CustomPortInput(nameof(transform), typeof(object), true)]
        public void GetInputs(List<SerializableEdge> edges)
        {
            if (edges.Count != 1)
            {
                transform = ProxyTransform.Null;
                return;
            }

            object data = edges[0].passThroughBuffer;
            Type dataType = edges[0].outputPort.fieldInfo.FieldType;

            InstanceID id;
            if (TypeHelper.TypeOf<GameObject>.Type.Equals(dataType))
            {
                GameObject gameObject = (GameObject)data;
                RecycleableMonobehaviour proxy = gameObject.GetComponentInChildren<RecycleableMonobehaviour>();
                if (proxy == null || !proxy.IsValid())
                {
                    transform = ProxyTransform.Null;
                    return;
                }

                id = proxy.entity;
            }
            else if (TypeHelper.InheritsFrom<Component>(dataType))
            {
                Component component = (Component)data;
                RecycleableMonobehaviour proxy = component.GetComponentInChildren<RecycleableMonobehaviour>();
                if (proxy == null || !proxy.IsValid())
                {
                    transform = ProxyTransform.Null;
                    return;
                }

                id = proxy.entity;
            }
            else if (TypeHelper.InheritsFrom<IEntityDataID>(dataType))
            {
                IEntityDataID entity = (IEntityDataID)data;
                id = entity.Idx;
            }
            else if (TypeHelper.InheritsFrom<IInstanceID>(dataType))
            {
                IInstanceID instance = (IInstanceID)data;
                id = new InstanceID(instance.Hash);
            }
            else
            {
                transform = ProxyTransform.Null;
                return;
            }

            transform = id.GetTransform();
        }
    }
}
