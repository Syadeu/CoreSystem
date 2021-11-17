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

namespace Syadeu.Presentation.Attributes
{
    /// <summary>
    /// <see cref="AttributeBase"/>에서 프록시 오브젝트에 대한 동작을 선언할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 이 인터페이스는 <seealso cref="IAttributeOnProxyCreated"/>, <seealso cref="IAttributeOnProxyRemoved"/>의 묶음 인터페이스입니다.
    /// </remarks>
    /// <remarks>
    /// 이 인터페이스 단일로는 동작하지않습니다. <seealso cref="AttributeProcessor"/>를 참조하는 class에 상속받아 사용되어야됩니다.
    /// </remarks>
    public interface IAttributeOnProxy : IAttributeOnProxyCreated, IAttributeOnProxyRemoved { }
}
