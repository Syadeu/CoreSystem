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

namespace Syadeu.Presentation
{
    /// <summary>
    /// <see cref="TargetScene"/> 에 자동으로 필요한 에셋을 등록합니다.
    /// </summary>
    /// <remarks>
    /// 사용자에 의해 수동으로 추가될 필요가 없습니다.
    /// </remarks>
    public interface INotifySceneAsset : INotifyAsset
    {
        [JsonIgnore]
        SceneReference TargetScene { get; }
    }
}
