﻿using Syadeu.Presentation.Internal;
using System.Collections.Generic;

namespace Syadeu.Presentation
{
    public interface IPresentationSystemGroup
    {
        /// <summary>
        /// 이 그룹내에서 실행되고 있는 <see cref="PresentationSystem{T}"/>들의 리스트를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 런타임 중 시스템 추가는 허용하지 않습니다.<br/>
        /// <seealso cref="PresentationRegisterEntity"/>를 상속받는 레지스터 클래스를 선언 후 등록하세요.
        /// </remarks>
        IReadOnlyList<PresentationSystemEntity> Systems { get; }

        /// <summary>
        /// 이 그룹내 모든 시스템(<seealso cref="PresentationSystem{T}"/>)을 실행합니다.
        /// </summary>
        void Start();
        /// <summary>
        /// 이 그룹내 모든 시스템(<seealso cref="PresentationSystem{T}"/>)을 정지합니다.
        /// </summary>
        void Stop();
    }
}
