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

using Newtonsoft.Json;
using Syadeu.Collections;
using System;

namespace Syadeu.Collections
{
    public interface IObject : ICloneable, IEquatable<IObject>
    {
        /// <summary>
        /// 이 오브젝트 엔티티의 이름입니다.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 이 오브젝트 엔티티의 오리지널 해쉬입니다.
        /// <see cref="EntityDataList"/>
        /// </summary>
        Hash Hash { get; }
        /// <summary>
        /// 이 오브젝트 엔티티의 인스턴스 고유 해쉬입니다.
        /// </summary>
        [JsonIgnore] InstanceID Idx { get; }

        int GetHashCode();
    }
}