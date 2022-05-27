// Copyright 2022 Seung Ha Kim
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

using System;
using UnityEngine;

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="UnityEngine.Object"/> 의 reletive path 를 담을 수 있습니다.
    /// </summary>
    [System.Serializable]
    public class AssetPathField : IEquatable<AssetPathField>
    {
        [SerializeField] protected string p_AssetPath = string.Empty;

        public virtual System.Type TargetType => TypeHelper.TypeOf<UnityEngine.Object>.Type;
        public string AssetPath
        {
            get => p_AssetPath;
            set => p_AssetPath = value;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor only
        /// </summary>
        public UnityEngine.Object EditorAsset
        {
            get
            {
                if (string.IsNullOrEmpty(p_AssetPath))
                {
                    return null;
                }

                return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p_AssetPath);
            }
            set
            {
                p_AssetPath = UnityEditor.AssetDatabase.GetAssetPath(value);
            }
        }
#endif

        public AssetPathField(string path)
        {
            p_AssetPath = path;
        }

        public bool Equals(AssetPathField other) => p_AssetPath.Equals(other.p_AssetPath);
    }
    /// <inheritdoc cref="AssetPathField"/>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class AssetPathField<T> : AssetPathField, IEquatable<AssetPathField<T>>
        where T : UnityEngine.Object
    {
        public override System.Type TargetType => TypeHelper.TypeOf<T>.Type;

#if UNITY_EDITOR
        /// <summary>
        /// Editor only
        /// </summary>
        public new T EditorAsset
        {
            get
            {
                if (string.IsNullOrEmpty(p_AssetPath))
                {
                    return null;
                }

                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(p_AssetPath);
            }
            set
            {
                p_AssetPath = UnityEditor.AssetDatabase.GetAssetPath(value);
            }
        }
#endif

        public AssetPathField(string path) : base(path) { }

        public bool Equals(AssetPathField<T> other) => p_AssetPath.Equals(other.p_AssetPath) && TargetType.Equals(other.TargetType);
    }
}
