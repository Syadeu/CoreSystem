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
using Syadeu.Collections.Graphs;
using Syadeu.Presentation.Entities;
using System;
using System.Reflection;
using UnityEngine;

namespace Syadeu.Presentation.Graphs
{
    [Serializable]
    public abstract class PresentationNode : BaseNode
    {
        [SerializeField, Input(name = "Link", allowMultiple = true)]
        protected ExecutableLink link = ExecutableLink.True;

        public override bool isRenamable => true;

        public override FieldInfo[] GetNodeFields()
        {
            var fields = base.GetNodeFields();
            Array.Sort(fields, Compararer);
            return fields;
        }
        private static int Compararer(FieldInfo x, FieldInfo y)
        {
            return x.Name == nameof(link) ? -1 : 1;
        }
    }
}
