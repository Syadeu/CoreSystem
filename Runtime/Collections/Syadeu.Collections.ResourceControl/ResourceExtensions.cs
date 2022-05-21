// Copyright 2021 Ikina Games
// Author : Seung Ha Kim (Syadeu)
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

#if UNITY_2019_1_OR_NEWER
#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !POINT_DISABLE_CHECKS
#define DEBUG_MODE
#endif
#define UNITYENGINE
#if UNITY_2019 && !UNITY_2020_1_OR_NEWER
#define UNITYENGINE_OLD
#else
using Syadeu.Collections.ResourceControl.LowLevel;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Diagnostics;
using Syadeu.Collections.Dignostics;
#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif
#endif

namespace Syadeu.Collections.ResourceControl
{
    public static class ResourceExtensions
    {
#if !UNITYENGINE_OLD
        #region Debug

        private static readonly Dictionary<Hash, Dictionary<Hash, StackFrame>> s_AssetLoadCallFrames = new Dictionary<Hash, Dictionary<Hash, StackFrame>>();
        [Conditional("DEBUG_MODE")]
        public static void AddDebugger(this in AssetInfo assetInfo)
        {
            if (!assetInfo.IsValid())
            {
                //"? not valid".ToLogError();
                return;
            }

            if (!s_AssetLoadCallFrames.TryGetValue(assetInfo.m_Key, out var list))
            {
                list = new Dictionary<Hash, StackFrame>();
                s_AssetLoadCallFrames.Add(assetInfo.m_Key, list);
            }
            list[assetInfo.m_InstanceID] = (ScriptUtils.GetCallerFrame(2));
        }
        [Conditional("DEBUG_MODE")]
        public static void AddDebugger(this in AssetInfo assetInfo, StackFrame stackFrame)
        {
            if (!assetInfo.IsValid())
            {
                //"? not valid".ToLogError();
                return;
            }

            if (!s_AssetLoadCallFrames.TryGetValue(assetInfo.m_Key, out var list))
            {
                list = new Dictionary<Hash, StackFrame>();
                s_AssetLoadCallFrames.Add(assetInfo.m_Key, list);
            }
            list[assetInfo.m_InstanceID] = (stackFrame);
        }
        [Conditional("DEBUG_MODE")]
        public static void RemoveLoadedFrame(this in AssetInfo t)
        {
            if (!t.IsValid())
            {
                //"? not valid".ToLogError();
                return;
            }

            if (!s_AssetLoadCallFrames.TryGetValue(t.m_Key, out var stackFrames)) return;

            stackFrames.Remove(t.m_InstanceID);
        }

        internal static Dictionary<Hash, StackFrame> GetLoadedFrame(this in UnsafeAssetInfo t)
        {
            if (!s_AssetLoadCallFrames.TryGetValue(Hash.NewHash(t.key.ToString()), out var stackFrames))
            {
                $"{t.key} not found".ToLogError();
                foreach (var item in s_AssetLoadCallFrames.Keys)
                {
                    $"{item}".ToLog();
                }
                return null;
            }
            return stackFrames;
        }
        public static StackFrame GetLoadedFrame(this in AssetInfo assetInfo)
        {
            if (!assetInfo.IsValid())
            {
                //"? not valid".ToLogError();
                return null;
            }

            if (!s_AssetLoadCallFrames.TryGetValue(assetInfo.m_Key, out var stackFrames))
            {
                return null;
            }
            return stackFrames[assetInfo.m_InstanceID];
        }

#if UNITY_EDITOR
        public static UnityEditor.MonoScript LoadScriptFile(StackFrame stackFrame)
        {
            string path = "Assets" + 
                stackFrame.GetFileName().Replace(Application.dataPath.Replace('/', Path.DirectorySeparatorChar), string.Empty);

            return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(path);
        }
#endif

        #endregion

        /// <summary>
        /// 에셋의 언로드 시기를 업데이트합니다.
        /// </summary>
        /// <param name="t"></param>
        public static void NotifyHandle(this AssetInfo t)
        {
            if (!t.IsValid())
            {
                //"? not valid".ToLogError();
                return;
            }

            t.UnsafeInfo.lastUsage = Timer.Start();
        }
        public static void NotifyHandle<T>(this AssetInfo<T> t)
            where T : UnityEngine.Object
        {
            if (!t.IsValid())
            {
                //"? not valid".ToLogError();
                return;
            }

            t.UnsafeInfo.lastUsage = Timer.Start();
        }

        public static void SetAssetHandle(this in AssetInfo t, AssetHandleType type)
        {
            if (!t.IsValid())
            {
                //"? not valid".ToLogError();
                return;
            }

            t.UnsafeInfo.assetHandleType = type;
        }
        public static void SetAssetHandle<T>(this in AssetInfo<T> t, AssetHandleType type)
            where T : UnityEngine.Object
        {
            if (!t.IsValid())
            {
                //"? not valid".ToLogError();
                return;
            }

            t.UnsafeInfo.assetHandleType = type;
        }
#endif
    }
}

#endif