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
using Syadeu.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    /// <summary>
    /// Scene 에 수동으로 필요한 에셋을 등록할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 이 구현부를 상속받은 객체는 <see cref="SceneSystem.RegisterSceneAsset(SceneReference, INotifyAsset)"/> 을 통해 등록할 수 있습니다.
    /// <br/>
    /// 자동 등록은 <seealso cref="INotifySceneAsset"/> 을 참조하세요.
    /// </remarks>
    public interface INotifyAsset
    {
        [JsonIgnore]
        IEnumerable<IPrefabReference> NotifyAssets { get; }
    }
}
