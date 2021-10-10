using Syadeu.Presentation.Proxy;
using System;

namespace Syadeu.Presentation.Actor
{
    [Flags]
    public enum UpdateType
    {
        Manual = 0,

        Lerp = 0b0001,
        Instant = 0b0010,

        /// <summary>
        /// 카메라와 오리엔테이션을 맞춥니다.
        /// </summary>
        SyncCameraOrientation = 0b0100,
        /// <summary>
        /// 부모(이 엔티티의 <seealso cref="ITransform"/>)와 오리엔테이션을 맞춥니다.
        /// </summary>
        SyncParentOrientation = 0b1000
    }
}
