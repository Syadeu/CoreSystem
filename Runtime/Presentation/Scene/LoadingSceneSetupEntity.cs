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

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Syadeu.Presentation
{
    /// <summary>
    /// 로딩 씬을 세팅하기 위한 셋업 Entity 클래스입니다.
    /// </summary>
    /// <remarks>
    /// 새로운 로딩 별 메소드를 설정하려면 <see cref="LoadingSceneSetupEntity.Start"/> 를 override 하여 사용하세요.
    /// </remarks>
    public abstract class LoadingSceneSetupEntity : MonoBehaviour
    {
        /// <summary>
        /// <see langword="true"/>일 경우, 이 셋업 컴포넌트가 시작하는 동시에 등록을 합니다.
        /// </summary>
        [SerializeField] private bool m_StartOnAwake = true;
        [Space]
        
        [Tooltip("로딩 콜이 실행되었을때 맨 처음으로 발생하는 이벤트입니다.")]
        [SerializeField] protected UnityEvent m_OnLoadingEnter;
        [Tooltip("로딩이 시작되기전 잠시 대기될때 실행되는 이벤트입니다.\n\n" +
            "arg1: 시작된 후부터 지나간 시간(초)\n" +
            "arg2: 기다리는 최종 타겟 시간(초)")]
        [SerializeField] protected UnityEvent<float, float> OnWaitLoading;
        [Tooltip("타겟 씬이 실제 로딩되는 중에 실행되는 이벤트입니다.\n\n" +
            "arg1: 타겟씬의 실제 로딩 결과 0 ~ 1")]
        [SerializeField] protected UnityEvent<float> m_OnLoading;
        [Tooltip("로딩이 끝난 후, 게임에게 로딩이 끝났음을 알리는 콜이 발생할때까지 실행되는 이벤트입니다.\n\n" +
            "arg1: 시작된 후부터 지나간 시간(초)\n" +
            "arg2: 기다리는 최종 타겟 시간(초)")]
        [SerializeField] protected UnityEvent<float,float> OnAfterLoading;
        [Tooltip("로딩 과정이 전부 끝났을때 호출되는 이벤트입니다.")]
        [SerializeField] protected UnityEvent m_OnLoadingExit;

        /// <summary>
        /// 이곳에서 <see cref="m_StartOnAwake"/> 변수를 이용한 동작이 일어납니다.<br/>
        /// 해당 부분을 피하고 싶으면 override 하고, base.Start() 를 지우세요.
        /// </summary>
        protected virtual void Start()
        {
            if (m_StartOnAwake) Initialize();
        }
        /// <summary>
        /// 이 설정을 <see cref="SceneSystem"/> 에 등록합니다.
        /// </summary>
        protected void Initialize() => CoreSystem.StartUnityUpdate(this, InternalInitialize());
        private IEnumerator InternalInitialize()
        {
            yield return PresentationSystem<SceneSystem>.GetAwaiter();
            ApplySetup();

            //Destroy(gameObject);
        }
        private void ApplySetup()
        {
            SceneSystem sceneSystem = PresentationSystem<SceneSystem>.System;
            sceneSystem.SetLoadingScene(
                m_OnLoadingEnter.Invoke,
                OnWaitLoading.Invoke,
                m_OnLoading.Invoke,
                OnAfterLoading.Invoke,
                m_OnLoadingExit.Invoke
                );
        }
    }
}
