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

using Syadeu.Collections.Threading;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace Syadeu.Collections
{
    public sealed class CoreApplication : StaticMonobehaviour<CoreApplication>
    {
        #region Statics

        [Preserve, RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            CoreApplication app = Instance;
        }
#if UNITY_EDITOR
        //[UnityEditor.InitializeOnLoadMethod]
        //public static void EditorInitialize()
        //{
        //    CollectionUtility.Initialize();
        //}
#endif

        private static bool s_IsShutdown = false;
        private static float s_InActiveTime;

        public static bool IsShutdown => s_IsShutdown;

        public static float InActiveTime
        {
            get => s_InActiveTime;
            set
            {
                PlayerPrefs.SetFloat("CoreSystem_InActiveTime", value);
                s_InActiveTime = value;
            }
        }

        #endregion

        protected override bool EnableLog => false;
        //protected override bool HideInInspector => !PointSettings.Instance.m_DisplayMainApplication;
        protected override bool HideInInspector => false;

        private ThreadInfo m_MainThread;
#if ENABLE_INPUT_SYSTEM
        private Timer m_InActiveTimer;
        private bool m_IsInActive = false;
        public event Action<bool> OnInActive;
#endif

        public ThreadInfo MainThread => m_MainThread;

        public event Action OnFrameUpdate;
        public event Action OnLateUpdate;
        public event Action OnApplicationShutdown;

        protected override void OnInitialize()
        {
            const string c_Instance = "Instance";

            //CollectionUtility.Initialize();
            s_InActiveTime = PlayerPrefs.GetFloat("CoreSystem_InActiveTime");

            Type[] types = TypeHelper.GetTypes(other => TypeHelper.TypeOf<IStaticInitializer>.Type.IsAssignableFrom(other));
            for (int i = 0; i < types.Length; i++)
            {
                if (TypeHelper.TypeOf<IStaticMonobehaviour>.Type.IsAssignableFrom(types[i]))
                {
                    types[i].GetProperty(c_Instance, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                        .GetValue(null);
                }
                else System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(types[i].TypeHandle);
            }
        }

        private void Awake()
        {
            m_MainThread = ThreadInfo.CurrentThread;
#if UNITY_EDITOR
            CoreHelper.s_EditorLogs = string.Empty;
#endif

#if ENABLE_INPUT_SYSTEM
            m_InActiveTimer = Timer.Start();
            UnityEngine.InputSystem.InputSystem.onActionChange += InputSystem_onActionChange;
#endif
        }

#if ENABLE_INPUT_SYSTEM

        private void InputSystem_onActionChange(object arg1, UnityEngine.InputSystem.InputActionChange arg2)
        {
            switch (arg2)
            {
                case UnityEngine.InputSystem.InputActionChange.ActionEnabled:
                    break;
                case UnityEngine.InputSystem.InputActionChange.ActionDisabled:
                    break;
                case UnityEngine.InputSystem.InputActionChange.ActionMapEnabled:
                    break;
                case UnityEngine.InputSystem.InputActionChange.ActionMapDisabled:
                    break;
                case UnityEngine.InputSystem.InputActionChange.ActionStarted:
                    break;
                case UnityEngine.InputSystem.InputActionChange.ActionPerformed:
                    UnityEngine.InputSystem.InputAction inputAction = (UnityEngine.InputSystem.InputAction)arg1;
                    if (inputAction.IsMouseMoveAction())
                    {
                        break;
                    }

                    m_InActiveTimer.Reset();

                    if (m_IsInActive)
                    {
                        m_IsInActive = false;
                        OnInActive?.Invoke(false);
                        //EventBroadcaster.PostEvent(ApplicationInActiveEvent.GetEvent(false));
                    }

                    break;
                case UnityEngine.InputSystem.InputActionChange.ActionCanceled:
                    break;
                case UnityEngine.InputSystem.InputActionChange.BoundControlsAboutToChange:
                    break;
                case UnityEngine.InputSystem.InputActionChange.BoundControlsChanged:
                    break;
                default:
                    break;
            }
        }

#endif

        private void OnApplicationFocus(bool focus)
        {
            if (IsShutdown) return;

            //EventBroadcaster.PostEvent(ApplicationOutFocusEvent.GetEvent(focus));
        }
        private void Update()
        {
            OnFrameUpdate?.Invoke();

#if ENABLE_INPUT_SYSTEM
            InActiveHandler();
#endif
        }
        private void LateUpdate()
        {
            OnLateUpdate?.Invoke();
        }
        protected override void OnShutdown()
        {
            s_IsShutdown = true;
            
            OnApplicationShutdown?.Invoke();

            //if (PointSettings.Instance.m_EnableLogFile)
            //{
            //    PointHelper.s_LogHandler.CloseLogFile();
            //}
        }

#if ENABLE_INPUT_SYSTEM
        private void InActiveHandler()
        {
            if (m_IsInActive) return;
            else if (m_InActiveTimer.ElapsedTime > InActiveTime)
            {
                m_IsInActive = true;
                OnInActive?.Invoke(true);
                //EventBroadcaster.PostEvent(ApplicationInActiveEvent.GetEvent(true));
            }
        }
#endif
    }
}
