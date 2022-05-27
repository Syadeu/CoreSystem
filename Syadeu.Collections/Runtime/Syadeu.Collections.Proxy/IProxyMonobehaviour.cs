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

using System;

namespace Syadeu.Collections.Proxy
{
    public interface IProxyMonobehaviour : IValidation
    {
        UnityEngine.GameObject gameObject { get; }
        UnityEngine.Transform transform { get; }

        bool InitializeOnCall { get; }

        bool Activated { get; }

        T GetOrAddComponent<T>() where T : UnityEngine.Component;
        T GetComponent<T>() where T : UnityEngine.Component;
        T[] GetComponents<T>() where T : UnityEngine.Component;
        UnityEngine.Component GetComponent(Type t);
        UnityEngine.Component[] GetComponents(Type t);

        T GetComponentUnity<T>() where T : UnityEngine.Component;

        T AddComponent<T>() where T : UnityEngine.Component;
    }
}
