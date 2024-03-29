﻿// Copyright 2021 Seung Ha Kim
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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Syadeu.Collections
{
    /// <summary>
    /// <see cref="EntitySystem"/>을 통해 생성(혹은 생성되지 않은 저장된 데이터)한 엔티티 구조의 제일 하단 객체입니다.
    /// </summary>
    /// <remarks>
    /// class 맴버 선언을 그리 추천하고 싶지 않지만, 필요에 의해 선언이 내부에 되었다면,<br/>
    /// 해당 값을 복사하여 인스턴스를 만들기 위해 <see cref="ObjectBase.Copy"/>을 override 하여 해당 값을 복사하여야합니다.
    /// <br/>
    /// 이 인터페이스의 직접 상속은 허용하지않습니다.<br/>
    /// 오브젝트를 선언하고싶다면 <seealso cref="Entities.EntityDataBase"/>를 상속하세요.
    /// </remarks>
    public interface IEntityData : IObject, IValidation
    {
        /// <summary>
        /// 이 엔티티가 가지고 있는 <see cref="AttributeBase"/> 배열입니다.
        /// </summary>
        [JsonIgnore] IAttribute[] Attributes { get; }
        ///// <summary>
        ///// 이 엔티티가 <see cref="EntitySystem"/>을 통해 생성된 객체인지 반환합니다.<br/>
        ///// <see langword="false"/> 일 경우, raw 데이터를 의미합니다.
        ///// </summary>
        //[JsonIgnore] bool isCreated { get; }

        bool HasAttribute(Hash attributeHash);
        bool HasAttribute(Type attributeType);
        bool HasAttribute<T>() where T : class, IAttribute;
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입의 어트리뷰트를 가져옵니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        IAttribute GetAttribute(Type t);
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입 <paramref name="t"/>의 어트리뷰트들을 가져옵니다.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        IAttribute[] GetAttributes(Type t);
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입 <typeparamref name="T"/>의 어트리뷰트를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// 만약 여러 개의 중복된 타입의 어트리뷰트를 가지고 있다면, 배열의 첫번째 어트리뷰트를 반환합니다.
        /// </remarks>
        T GetAttribute<T>() where T : class, IAttribute;
        /// <summary>
        /// 이 엔티티가 가지고있는 해당 타입 <typeparamref name="T"/>의 어트리뷰트들을 가져옵니다.
        /// </summary>
        T[] GetAttributes<T>() where T : class, IAttribute;
    }
}
