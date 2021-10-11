using Newtonsoft.Json;
using Syadeu.Collections;
using System;

namespace Syadeu.Presentation
{
    public interface IObject : ICloneable, IEquatable<IObject>
    {
        /// <summary>
        /// 이 오브젝트 엔티티의 이름입니다.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 이 오브젝트 엔티티의 오리지널 해쉬입니다.
        /// <see cref="EntityDataList"/>
        /// </summary>
        Hash Hash { get; }
        /// <summary>
        /// 이 오브젝트 엔티티의 인스턴스 고유 해쉬입니다.
        /// </summary>
        [JsonIgnore] Hash Idx { get; }

        int GetHashCode();
    }
}
