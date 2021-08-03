﻿using Syadeu.Mono;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Attributes
{
    /// <summary><inheritdoc cref="IAttributeOnProxyCreated"/></summary>
    /// <remarks>
    /// 이 인터페이스는 동기 작업입니다. 비동기 작업은 <seealso cref="IAttributeOnProxyCreated"/>을 참조하세요.
    /// </remarks>
    public interface IAttributeOnProxyCreatedSync
    {
        /// <summary><inheritdoc cref="IAttributeOnProxyCreated.OnProxyCreated(AttributeBase, Entity{IEntity}, RecycleableMonobehaviour)"/></summary>
        void OnProxyCreatedSync(AttributeBase attribute, Entity<IEntity> entity, RecycleableMonobehaviour monoObj);
    }
}
