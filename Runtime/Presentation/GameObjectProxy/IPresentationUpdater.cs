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


namespace Syadeu.Presentation.Proxy
{
    /// <summary>
    /// <see cref="RecycleableMonobehaviour"/> 의 하위 오브젝트가 가진 컴포넌트가 가지고 있으면 매 프레젠테이션 업데이트를 수행합니다.
    /// </summary>
    public interface IPresentationUpdater
    {
        void OnPresentation();
    }
}
