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

using Syadeu.Collections;
using Syadeu.Presentation.Internal;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    /// <summary xml:lang="ko">
    /// <seealso cref="PresentationManager"/>에서 수행할 시스템의 Entity 클래스입니다.
    /// </summary>
    /// <summary xml:lang="en">
    /// <seealso cref="PresentationManager"/>
    /// </summary>
    /// <remarks>
    /// 인스턴스를 생성하여 인스턴스 값으로만 작동하도록 제작되었습니다.<br/>
    /// <see langword="static"/> 값이 있으면 <see cref="Dispose"/>를 override 하여 해당 값을 초기화하세요.<br/>
    /// C#에서는 클래스가 소멸해도 해당 인스턴스의 <see langword="static"/> 필드, 프로퍼티 값은 메모리에서 방출되지 않습니다.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public abstract class PresentationSystemEntity<T> : PresentationSystemEntity, IEquatable<PresentationSystemEntity<T>>
        where T : PresentationSystemEntity
    {
        private readonly List<UnityEngine.GameObject> m_CreatedGameObjects = new List<UnityEngine.GameObject>();

        public new PresentationSystemID<T> SystemID => new PresentationSystemID<T>(m_GroupIndex, m_SystemIndex);

        public override bool IsStartable => true;

        protected override PresentationResult OnInitialize() { return PresentationResult.Normal; }
        protected override PresentationResult OnInitializeAsync() { return PresentationResult.Normal; }

        protected override PresentationResult OnStartPresentation() { return PresentationResult.Normal; }

        protected override PresentationResult BeforePresentation() { return PresentationResult.Normal; }
        protected override PresentationResult BeforePresentationAsync() { return PresentationResult.Normal; }

        protected override PresentationResult OnPresentation() { return PresentationResult.Normal; }
        protected override PresentationResult OnPresentationAsync() { return PresentationResult.Normal; }

        protected override PresentationResult AfterPresentation() { return PresentationResult.Normal; }
        protected override PresentationResult AfterPresentationAsync() { return PresentationResult.Normal; }

        internal override sealed void InternalOnDispose()
        {
            for (int i = 0; i < m_CreatedGameObjects.Count; i++)
            {
                Destroy(m_CreatedGameObjects[i]);
            }
            m_CreatedGameObjects.Clear();
        }
        public override void OnDispose() { }

        /// <summary>
        /// <see cref="OnInitialize"/> 혹은 <see cref="OnInitializeAsync"/> 에서만 수행되야됩니다.
        /// </summary>
        /// <typeparam name="TSystem"></typeparam>
        /// <param name="setter"></param>
        [Obsolete]
        protected void RequestSystem<TSystem>(Action<TSystem> setter
#if DEBUG_MODE
            , [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TSystem : PresentationSystemEntity
            => PresentationManager.RegisterRequest<DefaultPresentationGroup, TSystem>(setter
#if DEBUG_MODE
                , methodName
#endif
                );
        /// <summary>
        /// 시스템을 요청합니다. <typeparamref name="TGroup"/> 은 요청할 <typeparamref name="TSystem"/>이 속한 그룹입니다.
        /// </summary>
        /// <remarks>
        /// <seealso cref="OnInitialize"/> 혹은 <seealso cref="OnInitializeAsync"/> 에서만 수행되어야합니다.<br/>
        /// 기본 시스템 그룹은 <seealso cref="DefaultPresentationGroup"/> 입니다.
        /// </remarks>
        /// <typeparam name="TGroup"></typeparam>
        /// <typeparam name="TSystem"></typeparam>
        /// <param name="setter"></param>
        protected void RequestSystem<TGroup, TSystem>(Action<TSystem> setter
#if DEBUG_MODE
            , [System.Runtime.CompilerServices.CallerFilePath] string methodName = ""
#endif
            )
            where TGroup : PresentationGroupEntity
            where TSystem : PresentationSystemEntity
        {
            PresentationManager.RegisterRequest<TGroup, TSystem>(setter
#if DEBUG_MODE
                , methodName
#endif
                );
        }

        [System.Diagnostics.Conditional("DEBUG_MODE")]
        protected void DisposedCheck()
        {
            const string
                c_ErrorMsg = "You are trying to access an disposed system({0}). This is not allowed.";

            if (Disposed)
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    string.Format(c_ErrorMsg, TypeHelper.TypeOf<T>.ToString()));
            }
        }

        protected CoreRoutine StartCoroutine(IEnumerator cor) => CoreSystem.StartUnityUpdate(this, cor);
        protected CoreRoutine StartBackgroundCoroutine(IEnumerator cor) => CoreSystem.StartBackgroundUpdate(this, cor);
        protected void StopCoroutine(CoreRoutine routine) => CoreSystem.RemoveUnityUpdate(routine);
        protected void StopBackgroundCoroutine(CoreRoutine routine) => CoreSystem.RemoveBackgroundUpdate(routine);

        /// <summary>
        /// 씬에 종속되는 오브젝트를 생성하려면 <see cref="SceneSystem.CreateGameObject(string)"/> 을 먼저 고려하세요.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isStatic"></param>
        /// <returns></returns>
        protected UnityEngine.GameObject CreateGameObject(string name, bool isStatic)
        {
            CoreSystem.Logger.ThreadBlock(nameof(DontDestroyOnLoad), Syadeu.Internal.ThreadInfo.Unity);

            UnityEngine.GameObject obj = new UnityEngine.GameObject(name);

            if (isStatic)
            {
                DontDestroyOnLoad(obj);
            }

            return obj;
        }

        public bool Equals(PresentationSystemEntity<T> other) => m_GroupIndex.Equals(other.m_GroupIndex) && m_SystemIndex == other.m_SystemIndex;
    }
}
