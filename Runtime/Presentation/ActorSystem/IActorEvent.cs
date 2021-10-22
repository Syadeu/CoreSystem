using Syadeu.Collections;
using Syadeu.Presentation.Entities;

namespace Syadeu.Presentation.Actor
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// AOT 방지를 위해 <see cref="ActorSystem.AOTCodeGenerator{TEvent}"/> 를 사용하세요.
    /// </remarks>
    [UnityEngine.Scripting.RequireImplementors]
    public interface IActorEvent
    {
        bool BurstCompile { get; }

        void OnExecute(Entity<ActorEntity> from);
    }
}
