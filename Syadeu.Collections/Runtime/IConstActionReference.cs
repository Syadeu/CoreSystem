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

using Newtonsoft.Json;
using Syadeu.Collections.Converters;
using System;

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="IConstAction"/> 을 참조할 수 있는 레퍼런스 입니다.
    /// </summary>
    /// <remarks>
    /// 필드에 <seealso cref="ConstActionOptionsAttribute"/> 를 추가하여 추가적인 설정을 할 수 있습니다.
    /// </remarks>
    [JsonConverter(typeof(ConstActionReferenceJsonConverter))]
    public interface IConstActionReference : IEmpty
    {
        [JsonIgnore]
        Guid Guid { get; }
        [JsonIgnore]
        object[] Arguments { get; }

        void SetArguments(params object[] args);
    }
}
