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

namespace Syadeu
{
    public interface IInitialize
    {
        void Initialize();
    }
    public interface IInitialize<T>
    {
        void Initialize(T t);
    }
    public interface IInitialize<T, TA>
    {
        void Initialize(T t, TA ta);
    }
    public interface IInitialize<T, TA, TAA>
    {
        void Initialize(T t, TA ta, TAA taa);
    }
    public interface IInitialize<T, TA, TAA, TAAA>
    {
        void Initialize(T t, TA ta, TAA taa, TAAA taaa);
    }
}
