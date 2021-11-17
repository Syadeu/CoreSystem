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

namespace Syadeu.Entities
{
    public interface IStaticMonoManager : IStaticManager
    {
        /// <summary>
        /// Hierarchy에서 표시될 이름을 설정합니다.
        /// 런타임에 아무런 영향을 주지 않습니다.
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// true 일 경우, 씬이 전환되어도 파괴되지 않습니다.
        /// </summary>
        bool DontDestroy { get; }
        /// <summary>
        /// Hierarchy 에서 이 매니저 객체를 표시할지 결정합니다.
        /// <see cref="Mono.CoreSystemSettings.m_VisualizeObjects"/> 가 true일 경우 영향받지 않습니다.
        /// </summary>
        bool HideInHierarchy { get; }
        bool ManualInitialize { get; }

#pragma warning disable IDE1006 // Naming Styles
        UnityEngine.GameObject gameObject { get; }
        UnityEngine.Transform transform { get; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
